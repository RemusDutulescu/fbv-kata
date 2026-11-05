data "azurerm_client_config" "current" {}

locals {
  name = "funbooksandvideos"

  # SQL server names are global: a suffix derived from the subscription keeps this one unique.
  suffix = substr(sha1(data.azurerm_client_config.current.subscription_id), 0, 6)
}

resource "azurerm_resource_group" "main" {
  name     = "rg-${local.name}"
  location = var.location
}

resource "azurerm_log_analytics_workspace" "main" {
  name                = "log-${local.name}"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  sku                 = "PerGB2018"
  retention_in_days   = 30
}

resource "azurerm_application_insights" "main" {
  name                = "appi-${local.name}"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  workspace_id        = azurerm_log_analytics_workspace.main.id
  application_type    = "web"
}

# The API's identity. It signs in to Azure SQL with it, so there is no database password anywhere.
resource "azurerm_user_assigned_identity" "api" {
  name                = "id-${local.name}-api"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
}

resource "azurerm_mssql_server" "main" {
  name                = "sql-${local.name}-${local.suffix}"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  version             = "12.0"
  minimum_tls_version = "1.2"

  # ponytail: the API's identity is the server's Entra admin so that it can migrate at startup. Once a migration
  # bundle runs in the pipeline, make the pipeline the admin and give the API a database user with data rights only.
  azuread_administrator {
    login_username              = azurerm_user_assigned_identity.api.name
    object_id                   = azurerm_user_assigned_identity.api.principal_id
    azuread_authentication_only = true
  }
}

# ponytail: lets Azure services reach the server's public endpoint; signing in still needs Entra. A VNet-integrated
# Container Apps environment with a private endpoint for SQL closes it.
resource "azurerm_mssql_firewall_rule" "azure_services" {
  name             = "AllowAzureServices"
  server_id        = azurerm_mssql_server.main.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

resource "azurerm_mssql_database" "shop" {
  name        = "FunBooksAndVideos"
  server_id   = azurerm_mssql_server.main.id
  sku_name    = "Basic"
  max_size_gb = 2
}

resource "azurerm_container_app_environment" "main" {
  name                       = "cae-${local.name}"
  location                   = azurerm_resource_group.main.location
  resource_group_name        = azurerm_resource_group.main.name
  logs_destination           = "log-analytics"
  log_analytics_workspace_id = azurerm_log_analytics_workspace.main.id
}

resource "azurerm_container_app" "api" {
  name                         = "ca-${local.name}-api"
  container_app_environment_id = azurerm_container_app_environment.main.id
  resource_group_name          = azurerm_resource_group.main.name
  revision_mode                = "Single"

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.api.id]
  }

  secret {
    name  = "temporal-api-key"
    value = temporalcloud_apikey.api.token
  }

  secret {
    name  = "appinsights-connection-string"
    value = azurerm_application_insights.main.connection_string
  }

  ingress {
    external_enabled = true
    target_port      = 8080

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  # SQL must be reachable before the first replica starts and migrates it.
  depends_on = [azurerm_mssql_firewall_rule.azure_services]

  template {
    # One replica keeps it cheap. The rate limit lives in memory, so each extra replica would allow its own 30 a minute.
    min_replicas = 1
    max_replicas = 1

    container {
      name   = "api"
      image  = var.image
      cpu    = 0.5
      memory = "1Gi"

      env {
        name  = "ConnectionStrings__Shop"
        value = "Server=tcp:${azurerm_mssql_server.main.fully_qualified_domain_name},1433;Database=${azurerm_mssql_database.shop.name};Authentication=Active Directory Managed Identity;User Id=${azurerm_user_assigned_identity.api.client_id};Encrypt=True"
      }

      env {
        name  = "Temporal__Address"
        value = temporalcloud_namespace.main.endpoints.grpc_address
      }

      env {
        name  = "Temporal__Namespace"
        value = temporalcloud_namespace.main.id
      }

      env {
        name        = "Temporal__ApiKey"
        secret_name = "temporal-api-key"
      }

      env {
        name        = "APPLICATIONINSIGHTS_CONNECTION_STRING"
        secret_name = "appinsights-connection-string"
      }

      # Container Apps ends TLS and forwards plain HTTP. Trusting its X-Forwarded-Proto makes the app see https, so
      # the API docs send "Try it" requests over https instead of being blocked as mixed content.
      env {
        name  = "ASPNETCORE_FORWARDEDHEADERS_ENABLED"
        value = "true"
      }

      # Startup migrates the database before the API listens: allow it a minute before liveness counts.
      startup_probe {
        transport               = "HTTP"
        port                    = 8080
        path                    = "/health/live"
        interval_seconds        = 2
        failure_count_threshold = 30
      }

      liveness_probe {
        transport = "HTTP"
        port      = 8080
        path      = "/health/live"
      }

      # Readiness opens a SQL connection, which can take longer than the default one second.
      readiness_probe {
        transport = "HTTP"
        port      = 8080
        path      = "/health/ready"
        timeout   = 5
      }
    }
  }

  # The deploy workflow rolls out new images; Terraform owns everything else.
  lifecycle {
    ignore_changes = [template[0].container[0].image]
  }
}

output "api_url" {
  description = "The deployed API; its docs are at the root."
  value       = "https://${azurerm_container_app.api.ingress[0].fqdn}"
}

output "github_variables" {
  description = "Repository variables for the deploy workflow: Settings > Secrets and variables > Actions > Variables."
  value = {
    AZURE_CLIENT_ID       = azuread_application.deploy.client_id
    AZURE_TENANT_ID       = data.azurerm_client_config.current.tenant_id
    AZURE_SUBSCRIPTION_ID = data.azurerm_client_config.current.subscription_id
    AZURE_RESOURCE_GROUP  = azurerm_resource_group.main.name
    AZURE_API_APP         = azurerm_container_app.api.name
    AZURE_WORKER_APP      = azurerm_container_app.worker.name
  }
}

output "temporal_namespace" {
  description = "The Temporal Cloud namespace, to find the workflows in its UI."
  value       = temporalcloud_namespace.main.id
}

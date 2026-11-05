resource "temporalcloud_namespace" "main" {
  name           = local.name
  regions        = [var.temporal_region]
  api_key_auth   = true
  retention_days = 7
}

# The apps' service account can reach this namespace and nothing else in the account.
resource "temporalcloud_service_account" "app" {
  name = local.name

  namespace_scoped_access = {
    namespace_id = temporalcloud_namespace.main.id
    permission   = "write"
  }
}

resource "temporalcloud_apikey" "app" {
  display_name = local.name
  owner_type   = "service-account"
  owner_id     = temporalcloud_service_account.app.id
  expiry_time  = var.temporal_api_key_expiry
}

# The deploy workflow signs in to Azure with OpenID Connect: GitHub vouches for a run on the main branch, so no
# Azure secret is stored in GitHub at all.
resource "azuread_application" "deploy" {
  display_name = "${local.name}-github-deploy"
}

resource "azuread_service_principal" "deploy" {
  client_id = azuread_application.deploy.client_id
}

resource "azuread_application_federated_identity_credential" "main_branch" {
  application_id = azuread_application.deploy.id
  display_name   = "github-main-branch"
  audiences      = ["api://AzureADTokenExchange"]
  issuer         = "https://token.actions.githubusercontent.com"
  subject        = "repo:${var.github_repository}:ref:refs/heads/main"
}

# Contributor on the container app only: enough to roll out an image, but not to touch SQL, the firewall or the
# identities in the resource group.
resource "azurerm_role_assignment" "deploy" {
  scope                = azurerm_container_app.api.id
  role_definition_name = "Contributor"
  principal_id         = azuread_service_principal.deploy.object_id
}

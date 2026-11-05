# 7. Azure Container Apps, Azure SQL and Temporal Cloud

Status: accepted, 2026-11-05

## Context

The service should run somewhere real enough to use and cheap enough to leave running, built from code, with no
long-lived secrets to leak.

## Decision

- **Azure Container Apps** runs one container, with HTTPS ingress, health probes and a managed identity, and no
  Kubernetes to operate. It runs one replica, the cheapest setup; the README's limits say what scaling out would take.
- **Azure SQL** (Basic) is the same engine as local development and the tests. It uses Entra-only authentication:
  the app signs in with its managed identity, so no database password exists.
- **Temporal Cloud** is managed Temporal. The app authenticates with an API key owned by a service account that can
  reach only this namespace. The key is a Container Apps secret.
- **Application Insights** receives the OpenTelemetry traces.
- **GitHub Actions** delivers it. Deploy signs in to Azure through OpenID Connect federation, scoped to the main
  branch and to the container app, so no Azure secret is stored in GitHub. Actions are pinned by commit, and
  Dependabot keeps the pins current.
- **Terraform** builds all of it, with the azurerm, azuread and temporalcloud providers.

## Alternatives

- **App Service.** It would do, but it is a less natural home for a process that is also a long-running worker.
- **AKS.** Too much platform for one container.
- **Self-hosted Temporal.** It means running its database and its upgrades, which is work Temporal Cloud removes.

## Consequences

The shortcuts are listed in the README, and the first two are commented in [infra/main.tf](../../infra/main.tf):

- the app's identity is the SQL admin, so that it can migrate;
- the SQL endpoint is reachable by Azure services (signing in still needs Entra);
- the Terraform state is local and holds the Temporal key.

Each one has a named way past it: a migration bundle with a data-only user, a private endpoint, and a remote state
backend.

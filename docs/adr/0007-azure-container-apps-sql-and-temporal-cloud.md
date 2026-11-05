# 7. Azure Container Apps, Azure SQL and Temporal Cloud

Status: accepted, 2026-11-05

## Context

The service should run somewhere real enough to use and cheap enough to leave running, built from code, with no
long-lived secrets to leak.

## Decision

- **Azure Container Apps** runs the API and the worker as two apps (ADR 8), with a managed identity and no Kubernetes
  to operate. The API has HTTPS ingress and health probes, and one replica, the cheapest setup; the README's limits
  say what scaling it out would take. The worker has neither, and its replica count is a variable.
- **Azure SQL** (Basic) is the same engine as local development and the tests. It uses Entra-only authentication:
  the apps sign in with their managed identity, so no database password exists.
- **Temporal Cloud** is managed Temporal. The apps authenticate with an API key owned by a service account that can
  reach only this namespace. The key is a Container Apps secret.
- **Application Insights** receives the OpenTelemetry traces, metrics and logs.
- **GitHub Actions** delivers it. Deploy signs in to Azure through OpenID Connect federation, scoped to the main
  branch and to the two container apps, so no Azure secret is stored in GitHub. Actions are pinned by commit.
- **Terraform** builds all of it, with the azurerm, azuread and temporalcloud providers.

## Alternatives

- **App Service.** It would do for the API, but it is a less natural home for a long-running worker that serves no
  HTTP.
- **AKS.** Too much platform for two containers.
- **Self-hosted Temporal.** It means running its database and its upgrades, which is work Temporal Cloud removes.

## Consequences

The shortcuts, and the way past each, are in the README's [limits](../../README.md#limits-and-next-steps). The first
two are also commented in [infra/main.tf](../../infra/main.tf).

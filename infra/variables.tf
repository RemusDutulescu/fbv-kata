variable "image_prefix" {
  description = "Where the deploy workflow pushes the images, all lowercase: ghcr.io/<owner>/<repo>. The apps start with the latest -api and -worker images; the workflow rolls out the later ones."
  type        = string
}

variable "github_repository" {
  description = "The GitHub repository allowed to deploy, as its OIDC tokens name it: <owner>@<owner-id>/<name>@<repo-id> for a repository created after 15 July 2026, <owner>/<name> for an older one (README, deploy step 3)."
  type        = string
}

variable "worker_replicas" {
  description = "How many workers run the workflows and activities. They scale apart from the API (ADR 8)."
  type        = number
  default     = 1

  validation {
    # With no ingress to wake it, a worker app scaled to zero never starts again.
    condition     = var.worker_replicas >= 1
    error_message = "Run at least one worker."
  }
}

variable "location" {
  description = "Azure region."
  type        = string
  default     = "uksouth"
}

variable "temporal_region" {
  description = "Temporal Cloud region for the namespace. London on AWS sits next to Azure's UK South."
  type        = string
  default     = "aws-eu-west-2"
}

variable "temporal_api_key_expiry" {
  description = "When the apps' Temporal Cloud key expires (RFC 3339). Rotate it before then."
  type        = string
  default     = "2027-11-01T00:00:00Z"
}

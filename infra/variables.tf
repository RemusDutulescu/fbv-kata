variable "image" {
  description = "The API image the Container App starts with, e.g. ghcr.io/<owner>/funbooksandvideos:latest. The deploy workflow rolls out the later ones."
  type        = string
}

variable "github_repository" {
  description = "The GitHub repository allowed to deploy, as <owner>/<name>."
  type        = string
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
  description = "When the API's Temporal Cloud key expires (RFC 3339). Rotate it before then."
  type        = string
  default     = "2027-11-01T00:00:00Z"
}

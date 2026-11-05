terraform {
  required_version = ">= 1.10"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 5.12"
    }
    azuread = {
      source  = "hashicorp/azuread"
      version = "~> 3.10"
    }
    temporalcloud = {
      source  = "temporalio/temporalcloud"
      version = "~> 1.10"
    }
  }
}

# Azure: the signed-in Azure CLI account (az login), or ARM_* variables.
provider "azurerm" {
  features {}
}

provider "azuread" {}

# Temporal Cloud: an account API key in TEMPORAL_CLOUD_API_KEY.
provider "temporalcloud" {}

terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
    }
  }
}

resource "azurerm_static_web_app" "this" {
  name                = var.app_name
  resource_group_name = var.resource_group_name
  location            = var.location

  tags = {
    environment = var.environment
  }
}

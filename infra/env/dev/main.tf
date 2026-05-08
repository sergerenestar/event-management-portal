terraform {
  required_version = ">= 1.5"
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
    }
  }
  backend "azurerm" {
    resource_group_name  = "rg-eventportal-tfstate"
    storage_account_name = "cmfieventportaltfstate"
    container_name       = "tfstate"
    key                  = "dev.terraform.tfstate"
  }
}

provider "azurerm" {
  features {}
}

data "azurerm_client_config" "current" {}

locals {
  environment = "dev"
  prefix      = "eventportal"
}

resource "azurerm_resource_group" "this" {
  name     = "rg-${local.prefix}-${local.environment}"
  location = var.location

  tags = {
    environment = local.environment
    project     = local.prefix
    managed_by  = "terraform"
  }
}

module "monitoring" {
  source              = "../../modules/monitoring"
  resource_group_name = azurerm_resource_group.this.name
  location            = var.location
  environment         = local.environment
  workspace_name      = "law-${local.prefix}-${local.environment}"
  app_insights_name   = "appi-${local.prefix}-${local.environment}"
}

module "key_vault" {
  source                   = "../../modules/key_vault"
  resource_group_name      = azurerm_resource_group.this.name
  location                 = var.location
  environment              = local.environment
  vault_name               = "kv-${local.prefix}-${local.environment}"
  tenant_id                = data.azurerm_client_config.current.tenant_id
  app_service_principal_id = ""
}

module "storage" {
  source              = "../../modules/storage"
  resource_group_name = azurerm_resource_group.this.name
  location            = var.location
  environment         = local.environment
  account_name        = "stcmfieventportaldev"
}

module "sql_database" {
  source                       = "../../modules/sql_database"
  resource_group_name          = azurerm_resource_group.this.name
  location                     = var.location
  environment                  = local.environment
  server_name                  = "sql-${local.prefix}-${local.environment}"
  database_name                = "sqldb-${local.prefix}-${local.environment}"
  administrator_login          = var.sql_admin_login
  administrator_login_password = var.sql_admin_password
}

module "container_registry" {
  source              = "../../modules/container_registry"
  resource_group_name = azurerm_resource_group.this.name
  location            = var.location
  environment         = local.environment
  registry_name       = "acreventportaldev"
}

module "container_apps" {
  source                         = "../../modules/container_apps"
  resource_group_name            = azurerm_resource_group.this.name
  location                       = var.location
  environment                    = local.environment
  prefix                         = local.prefix
  log_analytics_workspace_id     = module.monitoring.workspace_id
  key_vault_uri                  = module.key_vault.vault_uri
  app_insights_connection_string = module.monitoring.app_insights_connection_string
  cors_allowed_origins           = "*"
}

resource "azurerm_key_vault_access_policy" "container_app" {
  key_vault_id = module.key_vault.vault_id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = module.container_apps.backend_principal_id

  secret_permissions = ["Get", "List"]
}

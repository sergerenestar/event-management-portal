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
    storage_account_name = "steventportaltfstate"
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

# Key Vault created before app_service so we can pass vault_uri into app settings.
# The app service's managed identity is granted access via a separate policy below.
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
  account_name        = "st${local.prefix}dev"
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

module "app_service" {
  source                         = "../../modules/app_service"
  resource_group_name            = azurerm_resource_group.this.name
  location                       = var.location
  environment                    = local.environment
  app_service_plan_name          = "plan-${local.prefix}-${local.environment}"
  app_name                       = "app-${local.prefix}-${local.environment}"
  key_vault_uri                  = module.key_vault.vault_uri
  app_insights_connection_string = module.monitoring.app_insights_connection_string
}

# Grant the App Service managed identity read access to Key Vault secrets
resource "azurerm_key_vault_access_policy" "app_service" {
  key_vault_id = module.key_vault.vault_id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = module.app_service.principal_id

  secret_permissions = ["Get", "List"]
}

module "static_web_app" {
  source              = "../../modules/static_web_app"
  resource_group_name = azurerm_resource_group.this.name
  location            = var.location
  environment         = local.environment
  app_name            = "stapp-${local.prefix}-${local.environment}"
}

variable "resource_group_name" { type = string }
variable "location" { type = string }
variable "environment" { type = string }
variable "app_service_plan_name" { type = string }
variable "app_name" { type = string }
variable "key_vault_uri" { type = string default = "" }
variable "app_insights_connection_string" { type = string default = "" }

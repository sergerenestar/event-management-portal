variable "resource_group_name"           { type = string }
variable "location"                       { type = string }
variable "environment"                    { type = string }
variable "prefix"                         { type = string }
variable "log_analytics_workspace_id"     { type = string }

variable "key_vault_uri" {
  type    = string
  default = ""
}

variable "app_insights_connection_string" {
  type      = string
  default   = ""
  sensitive = true
}

variable "cors_allowed_origins" {
  type    = string
  default = "*"
}

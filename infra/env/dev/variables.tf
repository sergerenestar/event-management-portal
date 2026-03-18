variable "location" {
  type        = string
  description = "Azure region for all resources"
  default     = "eastus"
}

variable "sql_admin_login" {
  type        = string
  description = "SQL Server administrator login name"
  default     = "sqladmin"
}

variable "sql_admin_password" {
  type        = string
  description = "SQL Server administrator password"
  sensitive   = true
}

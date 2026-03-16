variable "resource_group_name" { type = string }
variable "location" { type = string }
variable "environment" { type = string }
variable "server_name" { type = string }
variable "database_name" { type = string }
variable "administrator_login" { type = string }
variable "administrator_login_password" { type = string sensitive = true }

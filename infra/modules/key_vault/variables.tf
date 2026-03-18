variable "resource_group_name" {
  type = string
}

variable "location" {
  type = string
}

variable "environment" {
  type = string
}

variable "vault_name" {
  type = string
}

variable "tenant_id" {
  type = string
}

variable "app_service_principal_id" {
  type    = string
  default = ""
}

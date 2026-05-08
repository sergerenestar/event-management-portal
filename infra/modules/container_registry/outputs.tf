output "login_server" {
  description = "The login server URL of the container registry"
  value       = azurerm_container_registry.this.login_server
}

output "admin_username" {
  description = "The admin username for the container registry"
  value       = azurerm_container_registry.this.admin_username
  sensitive   = true
}

output "admin_password" {
  description = "The admin password for the container registry"
  value       = azurerm_container_registry.this.admin_password
  sensitive   = true
}

output "id" {
  description = "The resource ID of the container registry"
  value       = azurerm_container_registry.this.id
}

output "name" {
  description = "The name of the container registry"
  value       = azurerm_container_registry.this.name
}

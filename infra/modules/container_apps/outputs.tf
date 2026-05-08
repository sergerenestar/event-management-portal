output "backend_principal_id" {
  description = "System-assigned managed identity principal ID of the backend container app"
  value       = azurerm_container_app.backend.identity[0].principal_id
}

output "backend_fqdn" {
  description = "The FQDN of the backend container app"
  value       = azurerm_container_app.backend.latest_revision_fqdn
}

output "frontend_fqdn" {
  description = "The FQDN of the frontend container app"
  value       = azurerm_container_app.frontend.latest_revision_fqdn
}

output "backend_name" {
  description = "The name of the backend container app"
  value       = azurerm_container_app.backend.name
}

output "frontend_name" {
  description = "The name of the frontend container app"
  value       = azurerm_container_app.frontend.name
}

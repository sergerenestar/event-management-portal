output "principal_id" {
  description = "The system-assigned managed identity principal ID of the web app"
  value       = azurerm_linux_web_app.this.identity[0].principal_id
}

output "default_hostname" {
  description = "The default hostname of the web app"
  value       = azurerm_linux_web_app.this.default_hostname
}

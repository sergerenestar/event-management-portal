output "api_key" {
  description = "The API key used to authenticate with the Static Web App for deployments"
  value       = azurerm_static_web_app.this.api_key
  sensitive   = true
}

output "default_host_name" {
  description = "The default hostname of the Static Web App"
  value       = azurerm_static_web_app.this.default_host_name
}

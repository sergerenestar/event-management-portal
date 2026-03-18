output "workspace_id" {
  description = "The resource ID of the Log Analytics Workspace"
  value       = azurerm_log_analytics_workspace.this.id
}

output "app_insights_connection_string" {
  description = "The Application Insights connection string (sensitive)"
  value       = azurerm_application_insights.this.connection_string
  sensitive   = true
}

output "app_insights_instrumentation_key" {
  description = "The Application Insights instrumentation key (sensitive)"
  value       = azurerm_application_insights.this.instrumentation_key
  sensitive   = true
}

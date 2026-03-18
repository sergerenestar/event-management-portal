output "primary_blob_endpoint" {
  description = "The primary blob service endpoint URL"
  value       = azurerm_storage_account.this.primary_blob_endpoint
}

output "primary_connection_string" {
  description = "The primary connection string for the storage account (sensitive)"
  value       = azurerm_storage_account.this.primary_connection_string
  sensitive   = true
}

output "vault_uri" {
  description = "The URI of the Key Vault"
  value       = azurerm_key_vault.this.vault_uri
}

output "vault_id" {
  description = "The resource ID of the Key Vault"
  value       = azurerm_key_vault.this.id
}

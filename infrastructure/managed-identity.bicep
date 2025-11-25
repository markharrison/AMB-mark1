/*
  Managed Identity Bicep template
  Creates a User Assigned Managed Identity for the Expense Management System
*/

@description('Location for the managed identity')
param location string

@description('Base name for resources')
param baseName string

// Create User Assigned Managed Identity
resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'mid-${baseName}'
  location: location
}

// Outputs
output managedIdentityId string = managedIdentity.id
output managedIdentityName string = managedIdentity.name
output managedIdentityClientId string = managedIdentity.properties.clientId
output managedIdentityPrincipalId string = managedIdentity.properties.principalId

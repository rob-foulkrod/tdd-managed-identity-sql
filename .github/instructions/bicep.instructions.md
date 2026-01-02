---
applyTo: "**/*.bicep"
---

# Bicep Infrastructure Best Practices

When writing or editing Bicep files for Trainer Demo Deploy templates, follow these patterns to ensure consistency, security, and trainer-friendliness across the catalog.

## Naming Conventions

**Parameters and Variables:** Use lowerCamelCase
```bicep
param environmentName string
param principalId string
var resourceToken = toLower(uniqueString(...))
```

**Symbolic Names:** Use descriptive resource type names (not property names)
```bicep
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = { ... }  // ✅ Good
resource storageAccountName 'Microsoft.Storage/storageAccounts@2023-01-01' = { ... }  // ❌ Avoid 'name' suffix
```

**Resource Names:** Use abbreviations + resourceToken pattern
```bicep
var abbrs = loadJsonContent('./abbreviations.json')
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))
name: '${abbrs.storageStorageAccounts}${resourceToken}'
```

**Outputs:** Use UPPER_SNAKE_CASE
```bicep
output AZURE_LOCATION string = location
output APP_ENDPOINT string = app.properties.defaultHostName
```

## File Structure

### main.bicep Pattern

**Target Scope:** Use subscription level for azd templates
```bicep
targetScope = 'subscription'
```

**Standard Parameters:**
```bicep
@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of naming resource convention')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string

@description('Id of the user or app to assign application roles')
param principalId string = ''
```

**Required Tags:**
```bicep
var tags = {
  'azd-env-name': environmentName
  'SecurityControl': 'Ignore'  // Critical for MTT subscriptions
}
```

**Resource Group Creation:**
```bicep
resource rg 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: 'rg-${environmentName}'
  location: location
  tags: tags
}
```

**Module Invocation:**
```bicep
module resources './resources.bicep' = {
  scope: rg
  name: 'resources'
  params: {
    location: location
    tags: tags
    environmentName: environmentName
  }
}
```

### resources.bicep Pattern

**Target Scope:** Use resourceGroup level
```bicep
targetScope = 'resourceGroup'
```

**Parameter Defaults:**
```bicep
param location string = resourceGroup().location
param tags object = {}
param environmentName string
```

**Modularization:** For files >500 lines, break into domain modules (app/, core/, modules/)

## Parameters

**Always include descriptions:**
```bicep
@description('Size of the virtual machines')
param vmSize string = 'Standard_B2ms'
```

**Use constraints appropriately:**
```bicep
@minLength(1)
@maxLength(15)
param vmName string

@allowed(['Standard_B2ms', 'Standard_D2s_v3', 'Standard_D4s_v3'])
param vmSize string = 'Standard_B2ms'
```

**Secure sensitive values:**
```bicep
@secure()
@description('Administrator password for VMs')
param adminPassword string
```

**DO NOT use newGuid() for passwords:**
```bicep
param adminPassword string = newGuid()  // ❌ Anti-pattern: not persistent or usable
```

## Resource Definitions

**Use latest stable API versions:**
```bicep
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = { ... }
```

**Reference resources by symbolic name (not functions):**
```bicep
name: storageAccount.name  // ✅ Good
name: reference(storageAccountId).name  // ❌ Avoid
```

**Location handling:**
```bicep
location: location  // Use parameter
location: resourceGroup().location  // Or RG location
```

## Security Best Practices

**Use Managed Identities:**
```bicep
identity: {
  type: 'SystemAssigned'
}
```

**Implement RBAC:**
```bicep
resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(subscription().id, principalId, 'Storage Blob Data Reader')
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '2a2b9908-6ea1-4ae2-8e65-a410df84e7d1')
    principalId: principalId
  }
}
```

**Never expose secrets in outputs:**
```bicep
output connectionString string = storageAccount.properties.primaryEndpoints.blob  // ❌ Avoid
output STORAGE_ACCOUNT_NAME string = storageAccount.name  // ✅ Good - let app construct connection
```

**Firewall rules - avoid wide open access:**
```bicep
startIpAddress: '0.0.0.0'
endIpAddress: '255.255.255.255'  // ❌ Anti-pattern: allows entire internet
```

## Trainer-Friendly Patterns

**Feature flags for expensive resources:**
```bicep
@description('Whether to deploy Azure Bastion (adds ~$140/month)')
param deployBastion bool = false

module bastion './bastion.bicep' = if (deployBastion) {
  name: 'bastion'
  params: { ... }
}
```

**Cost-effective defaults:**
```bicep
@description('VM size for compute resources')
param vmSize string = 'Standard_B2ms'  // ✅ Good: affordable default

@description('App Service plan SKU')
param appServicePlanSku string = 'B1'  // ✅ Good: basic tier for demos
```

**Clear outputs for verification:**
```bicep
output AZURE_LOCATION string = location
output AZURE_TENANT_ID string = tenant().tenantId
output APP_ENDPOINT string = 'https://${app.properties.defaultHostName}'
output RESOURCE_GROUP_NAME string = rg.name
```

## Monitoring

**Include observability in production templates:**
```bicep
module monitoring './core/monitor/monitoring.bicep' = {
  name: 'monitoring'
  params: {
    logAnalyticsName: '${abbrs.operationalInsightsWorkspaces}${resourceToken}'
    applicationInsightsName: '${abbrs.insightsComponents}${resourceToken}'
    location: location
    tags: tags
  }
}
```

**Add diagnostic settings to resources:**
```bicep
diagnosticSettings: [
  {
    name: 'default'
    workspaceResourceId: monitoring.outputs.logAnalyticsWorkspaceResourceId
  }
]
```

## Code Quality

**Keep files manageable:**
- main.bicep: 50-300 lines (ideal: 100-200)
- resources.bicep: 100-500 lines (ideal: 200-400)
- If >1000 lines, refactor into modules

**Add helpful comments:**
```bicep
// Create storage account for Azure Functions
// Note: Requires unique name across all Azure
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = { ... }
```

**Use variables for complex expressions:**
```bicep
var storageAccountName = '${abbrs.storageStorageAccounts}${resourceToken}'
var appServicePlanName = '${abbrs.webServerFarms}${resourceToken}'

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName  // ✅ Good: clear and reusable
  // vs embedding expression in every property ❌
}
```

## Common Anti-Patterns to Avoid

❌ **Hardcoded credentials**
```bicep
param adminPassword string = 'Pa55w.rd1234'  // Never do this
```

❌ **Missing parameter descriptions**
```bicep
param environmentName string  // Always add @description
```

❌ **Inconsistent naming**
```bicep
param env_name string  // Use lowerCamelCase
param LOCATION string  // Use lowerCamelCase
```

❌ **Monolithic files**
```bicep
// Single 3000+ line file with all resources - break into modules
```

❌ **Missing SecurityControl tag**
```bicep
var tags = {
  'azd-env-name': environmentName
  // Missing 'SecurityControl': 'Ignore' - required for MTT subscriptions
}
```

## Quick Checklist

Before committing Bicep files:
- [ ] All parameters have `@description` decorators
- [ ] Secrets use `@secure()` decorator
- [ ] Tags include `SecurityControl: Ignore`
- [ ] Resource names use abbreviations + resourceToken
- [ ] Outputs are UPPER_SNAKE_CASE
- [ ] No hardcoded passwords or connection strings
- [ ] API versions are recent and stable
- [ ] Files are under 500 lines (or modularized)

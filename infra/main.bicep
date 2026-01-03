targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of naming resource convention')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string

@description('Public IP address to allow through the SQL Server firewall (optional)')
param whitelistPublicIp string = ''

@description('SQL Server administrator login name')
param sqlAdminLogin string = 'sqladmin'

@secure()
@description('SQL Server administrator password')
param sqlAdminPassword string

// Tags that should be applied to all resources.
// 
// Note that 'azd-service-name' tags should be applied separately to service host resources.
// Example usage:
//   tags: union(tags, { 'azd-service-name': <service name in azure.yaml> })
var tags = {
  'azd-env-name': environmentName
  SecurityControl: 'Ignore'  // Required for MTT managed subscriptions
}

var abbrs = loadJsonContent('./abbreviations.json')
var resourceToken = toLower(uniqueString(subscription().subscriptionId, environmentName, location))

// This deploys the Resource Group
resource rg 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: 'rg-${environmentName}'
  location: location
  tags: tags
}

// Monitoring: Log Analytics workspace + Application Insights
module monitoring './core/monitor/monitoring.bicep' = {
  name: 'monitoring'
  scope: rg
  params: {
    location: location
    tags: tags
    logAnalyticsName: '${abbrs.operationalInsightsWorkspaces}${resourceToken}'
    applicationInsightsName: '${abbrs.insightsComponents}${resourceToken}'
  }
}

// CONTRIBUTOR: Uncomment and customize the resources module below for your infrastructure.
// See .github/instructions/bicep.instructions.md for patterns and examples.
//
module resources './resources.bicep' = {
  name: 'resources'
  scope: rg
  params: {
    location: location
    tags: tags
    environmentName: environmentName
    whitelistPublicIp: whitelistPublicIp
    sqlAdminLogin: sqlAdminLogin
    sqlAdminPassword: sqlAdminPassword
    applicationInsightsName: monitoring.outputs.applicationInsightsName
    logAnalyticsWorkspaceId: monitoring.outputs.logAnalyticsWorkspaceId
  }
}

// Add outputs from the deployment here, if needed.
//
// This allows the outputs to be referenced by other bicep deployments in the deployment pipeline,
// or by the local machine as a way to reference created resources in Azure for local development.
// Secrets should not be added here.
//
// Outputs are automatically saved in the local azd environment .env file.
// To see these outputs, run `azd env get-values`,  or `azd env get-values --output json` for json output.
output AZURE_LOCATION string = location
output AZURE_TENANT_ID string = tenant().tenantId
output RESOURCE_GROUP_NAME string = rg.name
output APP_ENDPOINT string = resources.outputs.APP_ENDPOINT
output APP_SERVICE_NAME string = resources.outputs.APP_SERVICE_NAME
output SQL_SERVER_FQDN string = resources.outputs.SQL_SERVER_FQDN
output SQL_SERVER_NAME string = resources.outputs.SQL_SERVER_NAME
output SQL_DATABASE_NAME string = resources.outputs.SQL_DATABASE_NAME
output SYSTEM_ASSIGNED_MI_PRINCIPAL_ID string = resources.outputs.SYSTEM_ASSIGNED_MI_PRINCIPAL_ID
output USER_ASSIGNED_MI_CLIENT_ID string = resources.outputs.USER_ASSIGNED_MI_CLIENT_ID
output USER_ASSIGNED_MI_RESOURCE_ID string = resources.outputs.USER_ASSIGNED_MI_RESOURCE_ID
output USER_ASSIGNED_MI_NAME string = resources.outputs.USER_ASSIGNED_MI_NAME
output APPLICATIONINSIGHTS_CONNECTION_STRING string = monitoring.outputs.applicationInsightsConnectionString
output APPLICATIONINSIGHTS_NAME string = monitoring.outputs.applicationInsightsName
output LOGANALYTICS_WORKSPACE_ID string = monitoring.outputs.logAnalyticsWorkspaceId
output LOGANALYTICS_WORKSPACE_NAME string = monitoring.outputs.logAnalyticsWorkspaceName

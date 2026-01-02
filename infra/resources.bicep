targetScope = 'resourceGroup'

param environmentName string
param location string = resourceGroup().location
param tags object = {}

@description('Public IP address to allow through the SQL Server firewall (optional)')
param whitelistPublicIp string = ''

@description('SQL Server administrator login name')
param sqlAdminLogin string = 'sqladmin'

@secure()
@description('SQL Server administrator password')
param sqlAdminPassword string

// Load abbreviations for resource naming
var abbrs = loadJsonContent('./abbreviations.json')
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))

var appServicePlanName = '${abbrs.webServerFarms}${resourceToken}'
var appServiceName = '${abbrs.webSitesAppService}${resourceToken}'
var userAssignedIdentityName = '${abbrs.managedIdentityUserAssignedIdentities}${resourceToken}'
var sqlServerName = '${abbrs.sqlServers}${resourceToken}'
var sqlDatabaseName = '${abbrs.sqlServersDatabases}${resourceToken}'

resource userAssignedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
	name: userAssignedIdentityName
	location: location
	tags: tags
}

module appServicePlan './core/host/appserviceplan.bicep' = {
	name: 'appserviceplan'
	params: {
		name: appServicePlanName
		location: location
		tags: tags
		reserved: true
		sku: {
			name: 'B1'
			tier: 'Basic'
			capacity: 1
		}
	}
}

module webApp './core/host/appservice.bicep' = {
	name: 'webapp'
	params: {
		name: appServiceName
		location: location
		tags: union(tags, { 'azd-service-name': 'web' })
		appServicePlanId: appServicePlan.outputs.id
		managedIdentity: true

		runtimeName: 'dotnet'
		runtimeVersion: '8.0'
		linuxFxVersion: 'DOTNETCORE|8.0'

		appSettings: {
			Sql__Server: sqlServer.properties.fullyQualifiedDomainName
			Sql__Database: sqlServer::database.name
			ManagedIdentity__UserAssignedClientId: ''
		}
	}
}

resource sqlServer 'Microsoft.Sql/servers@2022-05-01-preview' = {
	name: sqlServerName
	location: location
	tags: tags
	properties: {
		version: '12.0'
		minimalTlsVersion: '1.2'
		publicNetworkAccess: 'Enabled'
		administratorLogin: sqlAdminLogin
		administratorLoginPassword: sqlAdminPassword
	}

	resource database 'databases' = {
		name: sqlDatabaseName
		location: location
		sku: {
			name: 'Basic'
			tier: 'Basic'
		}
		properties: {
			sampleName: 'AdventureWorksLT'
		}
	}

	// Allow Azure-hosted clients only
	resource allowAzureServices 'firewallRules' = {
		name: 'AllowAzureServices'
		properties: {
			startIpAddress: '0.0.0.0'
			endIpAddress: '0.0.0.0'
		}
	}

	// Optional: allow a specific public IP for instructor/dev workstation access.
	resource allowClientIp 'firewallRules' = if (!empty(whitelistPublicIp)) {
		name: 'AllowClientIp'
		properties: {
			startIpAddress: whitelistPublicIp
			endIpAddress: whitelistPublicIp
		}
	}
}

output APP_ENDPOINT string = webApp.outputs.uri
output APP_SERVICE_NAME string = webApp.outputs.name
output SQL_SERVER_FQDN string = sqlServer.properties.fullyQualifiedDomainName
output SQL_SERVER_NAME string = sqlServer.name
output SQL_DATABASE_NAME string = sqlServer::database.name
output SYSTEM_ASSIGNED_MI_PRINCIPAL_ID string = webApp.outputs.identityPrincipalId
output USER_ASSIGNED_MI_CLIENT_ID string = userAssignedIdentity.properties.clientId
output USER_ASSIGNED_MI_RESOURCE_ID string = userAssignedIdentity.id
output USER_ASSIGNED_MI_NAME string = userAssignedIdentity.name

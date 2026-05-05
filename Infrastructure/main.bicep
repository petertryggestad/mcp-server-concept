@description('The Azure region to deploy resources into')
param location string

@description('The name of the Azure Container Registry')
param acrName string

@description('The name of the Log Analytics Workspace')
param logAnalyticsName string

@description('The name of the Container Apps Environment')
param containerAppsEnvName string

@description('The name of the resource group to deploy into')
param resourceGroupName string

targetScope = 'subscription'

resource resourceGroup 'Microsoft.Resources/resourceGroups@2025-03-01' = {
  name: resourceGroupName
  location: location
}

module logAnalytics 'br/public:avm/res/operational-insights/workspace:0.11.2' = {
  name: 'logAnalytics'
  scope: resourceGroup
  params: {
    name: logAnalyticsName
    location: location
    skuName: 'PerGB2018'
  }
}

module containerAppsEnv 'br/public:avm/res/app/managed-environment:0.11.2' = {
  name: 'containerAppsEnv'
  scope: resourceGroup
  params: {
    name: containerAppsEnvName
    location: location
    zoneRedundant: false
    publicNetworkAccess: 'Enabled'
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.outputs.logAnalyticsWorkspaceId
        sharedKey: logAnalytics.outputs.primarySharedKey
      }
    }
    managedIdentities: { systemAssigned: true }
  }
}

module acr 'br/public:avm/res/container-registry/registry:0.9.1' = {
  name: 'acr'
  scope: resourceGroup
  params: {
    name: acrName
    location: location
    acrAdminUserEnabled: true
    acrSku: 'Basic'
    publicNetworkAccess: 'Enabled'
    networkRuleBypassOptions: 'AzureServices'
    roleAssignments: [
      {
        principalId: containerAppsEnv.outputs.systemAssignedMIPrincipalId
        principalType: 'ServicePrincipal'
        roleDefinitionIdOrName: 'AcrPull'
      }
      {
        principalId: deployer().objectId
        principalType: 'User'
        roleDefinitionIdOrName: 'AcrPush'
      }
    ]
  }
}

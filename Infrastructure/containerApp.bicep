@description('Environment Name - used to find the Container Apps Environment and ACR')
param environmentName string

@description('The name of the resource group to deploy into')
param resourceGroupName string

@description('The name of the container app to deploy')
param appName string

@description('The name of the image to deploy')
param imageName string

@description('Array of environment variables for the container app')
param environment array = []

@description('Minimum number of container app instances')
param minReplicas int = 0

targetScope = 'subscription'

resource resourceGroup 'Microsoft.Resources/resourceGroups@2025-04-01' existing = {
  name: resourceGroupName
}

resource managedEnvironment 'Microsoft.App/managedEnvironments@2025-01-01' existing = {
  name: environmentName
  scope: resourceGroup
}

module containerApp 'br/public:avm/res/app/container-app:0.19.0' = {
  name: 'containerAppDeployment'
  scope: resourceGroup
  params: {
    location: resourceGroup.location
    name: appName
    containers: [
      {
        name: appName
        image: '${environmentName}.azurecr.io/${imageName}:latest'
        resources: {
          cpu: '0.25'
          memory: '0.5Gi'
        }
        env: environment
      }
    ]
    registries: [
      {
        server: '${environmentName}.azurecr.io'
        identity: 'system-environment'
      }
    ]
    scaleSettings: {
      minReplicas: minReplicas
      maxReplicas: 10
      cooldownPeriod: 300
      pollingInterval: 30
    }
    environmentResourceId: managedEnvironment.id
    ingressTargetPort: 4547
    ingressTransport: 'auto'
    ingressExternal: true
    ingressAllowInsecure: false
    corsPolicy: {
      allowedOrigins: ['*']
      allowedHeaders: ['*']
    }
  }
}

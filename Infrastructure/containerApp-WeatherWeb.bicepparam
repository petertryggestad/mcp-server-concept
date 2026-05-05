using 'containerApp.bicep'

param imageName = 'weatherweb'
param appName = 'weatherweb'
param environmentName = 'mymcpenv'
param resourceGroupName = 'rg-mymcpenv'
param targetPort = 8080
param environment = []

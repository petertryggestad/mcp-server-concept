using 'containerApp.bicep'

param imageName = 'weatherforecast'
param appName = 'weatherforecast'
param environmentName = 'mymcpenv'
param resourceGroupName = 'rg-mymcpenv'
param keyVaultSecrets = [
  {
    key: 'weatherforecastclientid' // Must be lowercase - used in secretRef
    value: 'WeatherForecastClientId' // PascalCase - actual Key Vault secret name
  }
  {
    key: 'weatherforecastclientsecret' // Must be lowercase - used in secretRef
    value: 'WeatherForecastClientSecret' // PascalCase - actual Key Vault secret name
  }
  {
    key: 'weatherforecasttenantid' // Must be lowercase - used in secretRef
    value: 'WeatherForecastTenantId' // PascalCase - actual Key Vault secret name
  }
]
param environment = [
  {
    name: 'EntraIdAuth__TenantId'
    secretRef: 'weatherforecasttenantid'
  }
  {
    name: 'EntraIdAuth__ClientId'
    secretRef: 'weatherforecastclientid'
  }
  {
    name: 'EntraIdAuth__ClientSecret'
    secretRef: 'weatherforecastclientsecret'
  }
  {
    name: 'EntraIdAuth__PublicUrl'
    value: 'TODO-public-url-after-first-deploy'
  }
  {
    name: 'WeatherForecastApi__BaseUrl'
    value: 'https://api.met.no/weatherapi/locationforecast/2.0'
  }
  {
    name: 'IsTransportStateless'
    value: 'true'
  }
  // Application Insights connection string is automatically added by the template
]

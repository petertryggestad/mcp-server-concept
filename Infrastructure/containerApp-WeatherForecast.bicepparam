using 'containerApp.bicep'

param imageName = 'weatherforecast'
param appName = 'weatherforecast'
param environmentName = 'mymcpenv'
param resourceGroupName = 'rg-mymcpenv'
param environment = [
  {
    name: 'WeatherForecastApi__BaseUrl'
    value: 'https://api.met.no/weatherapi/locationforecast/2.0'
  }
]

param location string = 'westeurope'
param resourcePrefix string = 'testaks'

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2021-12-01-preview' = {
  name: '${resourcePrefix}registry'
  location: location
  sku:{
    name: 'Basic'
  }
}

module aks './aks-cluster.bicep' = {
  name: '${resourcePrefix}aks'
  params: {
    location: location
    clusterName: '${resourcePrefix}aks'
  }
}

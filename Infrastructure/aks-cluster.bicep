param location string
param clusterName string

param nodeCount int = 1
param vmSize string = 'standard_a2_v2'

resource aks 'Microsoft.ContainerService/managedClusters@2021-05-01' = {
  name: clusterName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    dnsPrefix: clusterName
    enableRBAC: true
    agentPoolProfiles: [
      {
        name: '${clusterName}ap1'
        count: nodeCount
        vmSize: vmSize
        mode: 'System'
      }
    ]
  }
}

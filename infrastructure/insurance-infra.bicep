// Optimized and Refactored Bicep template for multi-stage deployment

@description('Location for all resources.')
param location string = resourceGroup().location

@description('Base name for the Virtual Network.')
param vnetName string

@description('Name for the App Subnet.')
param appSubnetName string

@description('Name for the Function Subnet.')
param funcSubnetName string

@description('Name for the App Service Plan for Web Apps.')
param appServicePlanName string

@description('Name for the App Service Plan for Function Apps.')
param functionAppServicePlanName string

@description('Name for the Insurance Management Web App.')
param insuranceManagementAppName string

@description('Name for the Vehicle Insurance Web App.')
param vehicleInsuranceAppName string

@description('Name for the Billing Service Function App.')
param billingServiceFunctionName string

@description('Name for the Notification Service Function App.')
param notificationServiceFunctionName string

@description('Name for the Storage Account.')
param storageAccountName string

@description('Name for the SQL Server.')
param sqlServerName string

@description('Name for the SQL Database.')
param sqlDbName string = 'devsqldb'

@description('Admin username for the SQL Server.')
param sqlAdminUsernameParam string = 'sqladmin'

@description('Admin password for the SQL Server. This should be a secure string.')
@secure()
param sqlAdminPassword string

@description('Name for the Service Bus Namespace.')
param serviceBusNamespaceName string

@description('Name for the Application Insights instance.')
param appInsightsName string

@description('Name for the Log Analytics Workspace.')
param logAnalyticsWorkspaceName string

@description('The ASP.NET Core environment setting for the applications (e.g., Development, Staging, Production).')
param aspNetCoreEnvironment string = 'Development'

@description('SKU name for the App Service Plans (e.g., B1, P1V2).')
param appServicePlanSkuName string = 'B1'

@description('SKU tier for the App Service Plans (e.g., Basic, PremiumV2).')
param appServicePlanSkuTier string = 'Basic'

@description('SKU name for the Function App Service Plan (e.g., Y1, B1).')
param functionAppServicePlanSkuName string = 'B1'

@description('SKU tier for the Function App Service Plan (e.g., Dynamic, Basic).')
param functionAppServicePlanSkuTier string = 'Basic'

@description('SKU name for the SQL Database (e.g., Basic, S0, GP_Gen5_2).')
param sqlSkuName string = 'Basic'

@description('SKU name for the Service Bus Namespace (e.g., Basic, Standard, Premium).')
param serviceBusSkuName string = 'Basic'

@description('SKU name for the Log Analytics Workspace (e.g., PerGB2018, Free).')
param logAnalyticsSkuName string = 'PerGB2018'

@description('Retention period in days for Log Analytics Workspace.')
param logAnalyticsRetentionInDays int = 30

@description('SKU name for the Storage Account (e.g., Standard_LRS, Standard_GRS).')
param storageSkuName string = 'Standard_LRS'

// --- Variables ---
var appSubnetPrefix = '10.0.1.0/24'
var funcSubnetPrefix = '10.0.2.0/24'
var invoiceQueueName = 'invoice-email-notification-queue'
var invoiceGenQueueName = 'invoice-generation-queue'
var invoicesContainerName = 'invoices'
var dotnetLinuxFxVersion = 'DOTNETCORE|8.0'
var commonLogsDirectorySizeLimit = 100
var appInsightsExtensionVersion = '~3'
var functionsExtensionVersion = '~4'
var functionsWorkerRuntime = 'dotnet-isolated'

// --- Network Security Groups ---
resource appSubnetNsg 'Microsoft.Network/networkSecurityGroups@2024-05-01' = {
  name: '${appSubnetName}-nsg'
  location: location
  properties: {
    securityRules: [
      {
        name: 'AllowHTTP'
        properties: {
          priority: 100
          direction: 'Inbound'
          access: 'Allow'
          protocol: 'Tcp'
          sourcePortRange: '*'
          destinationPortRange: '80'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
        }
      }
      {
        name: 'AllowHTTPS'
        properties: {
          priority: 110
          direction: 'Inbound'
          access: 'Allow'
          protocol: 'Tcp'
          sourcePortRange: '*'
          destinationPortRange: '443'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
        }
      }
    ]
  }
}

resource funcSubnetNsg 'Microsoft.Network/networkSecurityGroups@2024-05-01' = {
  name: '${funcSubnetName}-nsg'
  location: location
  properties: {
    securityRules: []
  }
}

// --- Virtual Network ---
resource vnet 'Microsoft.Network/virtualNetworks@2024-05-01' = {
  name: vnetName
  location: location
  properties: {
    addressSpace: {
      addressPrefixes: ['10.0.0.0/16']
    }
    subnets: [
      {
        name: appSubnetName
        properties: {
          addressPrefix: appSubnetPrefix
          networkSecurityGroup: {
            id: appSubnetNsg.id
          }
          serviceEndpoints: [
            {
              service: 'Microsoft.Sql'
              locations: [location]
            }
          ]
          delegations: [
            {
              name: 'Microsoft.Web.serverFarms'
              properties: {
                serviceName: 'Microsoft.Web/serverFarms'
              }
            }
          ]
        }
      }
      {
        name: funcSubnetName
        properties: {
          addressPrefix: funcSubnetPrefix
          networkSecurityGroup: {
            id: funcSubnetNsg.id
          }
          serviceEndpoints: [
            {
              service: 'Microsoft.Sql'
              locations: [location]
            }
          ]
          delegations: [
            {
              name: 'Microsoft.Web.serverFarms'
              properties: {
                serviceName: 'Microsoft.Web/serverFarms'
              }
            }
          ]
        }
      }
    ]
  }
}

// --- Storage Account ---
resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: storageSkuName
  }
  kind: 'StorageV2'
  properties: {
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  name: 'default'
  parent: storage
}

resource invoicesBlobContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  name: invoicesContainerName
  parent: blobService
  properties: {
    publicAccess: 'None'
  }
}

// --- App Service Plans ---
resource appServicePlan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: appServicePlanSkuName
    tier: appServicePlanSkuTier
  }
  properties: {
    reserved: true
  }
}

resource functionAppServicePlan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: functionAppServicePlanName
  location: location
  sku: {
    name: functionAppServicePlanSkuName
    tier: functionAppServicePlanSkuTier
  }
  properties: {
    reserved: false // Windows hosting
  }
}

// --- SQL Server and Database ---
resource sqlServer 'Microsoft.Sql/servers@2021-11-01' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: sqlAdminUsernameParam
    administratorLoginPassword: sqlAdminPassword
    version: '12.0'
    publicNetworkAccess: 'Enabled'
    restrictOutboundNetworkAccess: 'Disabled'
    minimalTlsVersion: '1.2'
  }
}

resource sqlDb 'Microsoft.Sql/servers/databases@2021-11-01' = {
  name: sqlDbName
  parent: sqlServer
  location: location
  sku: {
    name: sqlSkuName
  }
  properties: {}
}

resource sqlFirewallRuleAllWindowsAzureIps 'Microsoft.Sql/servers/firewallRules@2021-11-01' = {
  name: 'AllowAllWindowsAzureIps'
  parent: sqlServer
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource sqlVNetRuleAppService 'Microsoft.Sql/servers/virtualNetworkRules@2021-11-01' = {
  name: 'AllowAppServiceSubnet'
  parent: sqlServer
  properties: {
    virtualNetworkSubnetId: '${vnet.id}/subnets/${appSubnetName}'
    ignoreMissingVnetServiceEndpoint: false
  }
}

resource sqlVNetRuleFunctions 'Microsoft.Sql/servers/virtualNetworkRules@2021-11-01' = {
  name: 'AllowFunctionSubnet'
  parent: sqlServer
  properties: {
    virtualNetworkSubnetId: '${vnet.id}/subnets/${funcSubnetName}'
    ignoreMissingVnetServiceEndpoint: false
  }
}

// --- Service Bus ---
resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2024-01-01' = {
  name: serviceBusNamespaceName
  location: location
  sku: {
    name: serviceBusSkuName
  }
  properties: {}
}

resource invoiceServiceBusQueue 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
  name: invoiceQueueName
  parent: serviceBusNamespace
  properties: {}
}

resource invoiceGenServiceBusQueue 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
  name: invoiceGenQueueName
  parent: serviceBusNamespace
  properties: {}
}

// --- Log Analytics and Application Insights ---
resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsWorkspaceName
  location: location
  properties: {
    sku: {
      name: logAnalyticsSkuName
    }
    retentionInDays: logAnalyticsRetentionInDays
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspace.id
  }
}

// --- Connection Strings and Keys (Variables) ---
var sqlDbConnStr = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${sqlDbName};User ID=${sqlAdminUsernameParam};Password=${sqlAdminPassword};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;ConnectRetryCount=3;ConnectRetryInterval=10;'
var storageConnStr = 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};AccountKey=${storage.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
var serviceBusConnStr = listKeys('${serviceBusNamespace.id}/AuthorizationRules/RootManageSharedAccessKey', serviceBusNamespace.apiVersion).primaryConnectionString
var appInsightsConnStr = appInsights.properties.ConnectionString

// --- Common Application Settings ---
var commonWebAppSettings = [
  {
    name: 'SqlConnectionString'
    value: sqlDbConnStr
  }
  {
    name: 'SqlDatabaseName'
    value: sqlDbName
  }
  {
    name: 'ServiceBusConnectionString'
    value: serviceBusConnStr
  }
  {
    name: 'ServiceBusNamespace'
    value: serviceBusNamespaceName
  }
  {
    name: 'ServiceBusQueueName'
    value: invoiceQueueName
  }
  {
    name: 'SvbusInvoiceGenQueueName'
    value: invoiceGenQueueName
  }
  {
    name: 'SvbusInvoiceEmailQueueName'
    value: invoiceQueueName
  }
  {
    name: 'StorageAccountName'
    value: storageAccountName
  }
  {
    name: 'StorageAccountConnectionString'
    value: storageConnStr
  }
  {
    name: 'InvoicesContainerName'
    value: invoicesContainerName
  }
  {
    name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
    value: appInsightsConnStr
  }
  {
    name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
    value: appInsightsExtensionVersion
  }
  {
    name: 'ASPNETCORE_ENVIRONMENT'
    value: aspNetCoreEnvironment
  }
]

var commonFunctionAppSettings = union(commonWebAppSettings, [
  {
    name: 'AzureWebJobsStorage'
    value: storageConnStr
  }
  {
    name: 'FUNCTIONS_EXTENSION_VERSION'
    value: functionsExtensionVersion
  }
  {
    name: 'FUNCTIONS_WORKER_RUNTIME'
    value: functionsWorkerRuntime
  }
])

// --- App Services and Function Apps ---
resource insuranceManagementApp 'Microsoft.Web/sites@2024-04-01' = {
  name: insuranceManagementAppName
  location: location
  kind: 'app'
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    virtualNetworkSubnetId: '${vnet.id}/subnets/${appSubnetName}'
    siteConfig: {
      linuxFxVersion: dotnetLinuxFxVersion
      alwaysOn: appServicePlanSkuTier != 'Basic' && appServicePlanSkuTier != 'Free' && appServicePlanSkuTier != 'Shared'
      ftpsState: 'FtpsOnly'
      minTlsVersion: '1.2'
      appSettings: commonWebAppSettings
      healthCheckPath: '/health'
      httpLoggingEnabled: true
      logsDirectorySizeLimit: commonLogsDirectorySizeLimit
    }
  }
  dependsOn: [invoicesBlobContainer, invoiceServiceBusQueue, invoiceGenServiceBusQueue, sqlDb]
}

resource vehicleInsuranceApp 'Microsoft.Web/sites@2024-04-01' = {
  name: vehicleInsuranceAppName
  location: location
  kind: 'app'
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    virtualNetworkSubnetId: '${vnet.id}/subnets/${appSubnetName}'
    siteConfig: {
      linuxFxVersion: dotnetLinuxFxVersion
      alwaysOn: appServicePlanSkuTier != 'Basic' && appServicePlanSkuTier != 'Free' && appServicePlanSkuTier != 'Shared'
      ftpsState: 'FtpsOnly'
      minTlsVersion: '1.2'
      appSettings: commonWebAppSettings
      healthCheckPath: '/health'
      httpLoggingEnabled: true
      logsDirectorySizeLimit: commonLogsDirectorySizeLimit
    }
  }
  dependsOn: [invoicesBlobContainer, invoiceServiceBusQueue, invoiceGenServiceBusQueue, sqlDb]
}

resource billingServiceFunction 'Microsoft.Web/sites@2024-04-01' = {
  name: billingServiceFunctionName
  location: location
  kind: 'functionapp'
  properties: {
    serverFarmId: functionAppServicePlan.id
    httpsOnly: true
    virtualNetworkSubnetId: '${vnet.id}/subnets/${funcSubnetName}'
    siteConfig: {
      netFrameworkVersion: 'v8.0' // .NET 8 for Windows
      appSettings: commonFunctionAppSettings
      ftpsState: 'FtpsOnly'
      minTlsVersion: '1.2'
      httpLoggingEnabled: true
      logsDirectorySizeLimit: commonLogsDirectorySizeLimit
    }
  }
  dependsOn: [invoicesBlobContainer, invoiceServiceBusQueue, invoiceGenServiceBusQueue, sqlDb]
}

resource notificationServiceFunction 'Microsoft.Web/sites@2024-04-01' = {
  name: notificationServiceFunctionName
  location: location
  kind: 'functionapp'
  properties: {
    serverFarmId: functionAppServicePlan.id
    httpsOnly: true
    virtualNetworkSubnetId: '${vnet.id}/subnets/${funcSubnetName}'
    siteConfig: {
      netFrameworkVersion: 'v8.0' // .NET 8 for Windows
      appSettings: commonFunctionAppSettings
      ftpsState: 'FtpsOnly'
      minTlsVersion: '1.2'
      httpLoggingEnabled: true
      logsDirectorySizeLimit: commonLogsDirectorySizeLimit
    }
  }
  dependsOn: [invoicesBlobContainer, invoiceServiceBusQueue, invoiceGenServiceBusQueue, sqlDb]
}

// --- Outputs ---
output ResourceGroupLocation string = location
output VirtualNetworkName string = vnet.name
output AppSubnetName string = appSubnetName
output FuncSubnetName string = funcSubnetName

output StorageAccountName string = storage.name
output StorageAccountConnectionString string = storageConnStr
output InvoicesContainerName string = invoicesContainerName

output SqlServerName string = sqlServer.name
output SqlServerFQDN string = sqlServer.properties.fullyQualifiedDomainName
output SqlDatabaseName string = sqlDb.name
output SqlConnectionString string = sqlDbConnStr

output ServiceBusNamespace string = serviceBusNamespace.name
output ServiceBusQueueName string = invoiceServiceBusQueue.name
output ServiceBusInvoiceGenQueueName string = invoiceGenServiceBusQueue.name
output ServiceBusConnectionString string = serviceBusConnStr
output ServiceBusEndpoint string = 'https://${serviceBusNamespace.name}.servicebus.windows.net/'

output AppServicePlanName string = appServicePlan.name
output FunctionAppServicePlanName string = functionAppServicePlan.name

output InsuranceManagementAppName string = insuranceManagementApp.name
output InsuranceManagementAppURL string = 'https://${insuranceManagementApp.properties.defaultHostName}'
output VehicleInsuranceAppName string = vehicleInsuranceApp.name
output VehicleInsuranceAppURL string = 'https://${vehicleInsuranceApp.properties.defaultHostName}'

output BillingServiceFunctionName string = billingServiceFunction.name
output BillingServiceFunctionURL string = 'https://${billingServiceFunction.properties.defaultHostName}'
output NotificationServiceFunctionName string = notificationServiceFunction.name
output NotificationServiceFunctionURL string = 'https://${notificationServiceFunction.properties.defaultHostName}'

output LogAnalyticsWorkspaceName string = logAnalyticsWorkspace.name
output LogAnalyticsWorkspaceId string = logAnalyticsWorkspace.id
output ApplicationInsightsName string = appInsights.name
output ApplicationInsightsConnectionString string = appInsightsConnStr

output ASPNETCORE_ENVIRONMENT_Output string = aspNetCoreEnvironment

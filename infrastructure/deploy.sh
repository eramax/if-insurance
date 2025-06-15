#!/bin/bash
# Usage: ./deployment.sh

set -e

# Prompt for environment/stage
read -p "Enter environment/stage [dev]: " STAGE
STAGE=${STAGE:-dev}

# Prompt for resource group
read -p "Enter resource group name [${STAGE}-insurance-rg]: " RG
RG=${RG:-${STAGE}-insurance-rg}

# Prompt for location
read -p "Enter Azure location [swedencentral]: " LOCATION
LOCATION=${LOCATION:-swedencentral}

read -s -p "Enter SQL admin password [ComplexPass123!]: " SQL_ADMIN_PASSWORD
SQL_ADMIN_PASSWORD=${SQL_ADMIN_PASSWORD:-ComplexPass123!}

echo
# Create resource group if it doesn't exist
az group show --name "$RG" >/dev/null 2>&1 || \
  az group create --name "$RG" --location "$LOCATION"

# Generate unique string (similar to Bicep's uniqueString function)
# This creates a deterministic hash based on the resource group ID
RG_ID=$(az group show --name "$RG" --query id -o tsv)
UNIQUE_STRING=$(echo -n "$RG_ID" | sha1sum | cut -c1-8)

echo "Generated unique string: $UNIQUE_STRING"
echo "This will be used as suffix for all resource names"

# Generate expected resource names using the stage-resource-suffix format
# Note: Storage accounts don't support dashes, so we use a different format for them
STORAGE_NAME="${STAGE}storage${UNIQUE_STRING}"  # No dashes for storage account
SQL_SERVER_NAME="${STAGE}-sql-${UNIQUE_STRING}"
SERVICEBUS_NAME="${STAGE}-sb-${UNIQUE_STRING}"
APPINSIGHTS_NAME="${STAGE}-appi-${UNIQUE_STRING}"
LOG_ANALYTICS_NAME="${STAGE}-law-${UNIQUE_STRING}"
INSURANCE_MGMT_APP="${STAGE}-insurance-${UNIQUE_STRING}"
VEHICLE_INSURANCE_APP="${STAGE}-vehicle-${UNIQUE_STRING}"
BILLING_FUNCTION="${STAGE}-billing-${UNIQUE_STRING}"
NOTIFICATION_FUNCTION="${STAGE}-notify-${UNIQUE_STRING}"

# Static resource names with stage prefix
VNET_NAME="${STAGE}-vnet"
APP_SUBNET_NAME="${STAGE}-app-subnet"
FUNC_SUBNET_NAME="${STAGE}-func-subnet"
APP_SERVICE_PLAN_NAME="${STAGE}-asplan-apps"
FUNCTION_APP_SERVICE_PLAN_NAME="${STAGE}-asplan-functions"
SQL_DB_NAME="${STAGE}sqldb"  # SQL DB names should avoid dashes for consistency

echo ""
echo "  Resource Group: $RG"
echo "  Location: $LOCATION"
echo "  Stage/Environment: $STAGE"
echo "  Unique String: $UNIQUE_STRING"
echo "  SQL Admin Password: [HIDDEN]"
echo ""
echo "=== Resource ==="
echo "  Storage Account: $STORAGE_NAME"
echo "  SQL Server: $SQL_SERVER_NAME"
echo "  SQL Database: $SQL_DB_NAME"
echo "  Service Bus: $SERVICEBUS_NAME"
echo "  App Insights: $APPINSIGHTS_NAME"
echo "  Log Analytics Workspace: $LOG_ANALYTICS_NAME"
echo "  Insurance Management App: $INSURANCE_MGMT_APP"
echo "  Vehicle Insurance App: $VEHICLE_INSURANCE_APP"
echo "  Billing Function: $BILLING_FUNCTION"
echo "  Notification Function: $NOTIFICATION_FUNCTION"
echo ""
echo "=== Static Resource ==="
echo "  Virtual Network: $VNET_NAME"
echo "  App Subnet: $APP_SUBNET_NAME"
echo "  Function Subnet: $FUNC_SUBNET_NAME"
echo "  App Service Plan (Web Apps): $APP_SERVICE_PLAN_NAME"
echo "  App Service Plan (Functions): $FUNCTION_APP_SERVICE_PLAN_NAME"
echo ""
echo "=== Service URLs ==="
echo "Insurance Management: https://$INSURANCE_MGMT_APP.azurewebsites.net"
echo "Vehicle Insurance: https://$VEHICLE_INSURANCE_APP.azurewebsites.net"
echo "Billing Function: https://$BILLING_FUNCTION.azurewebsites.net"
echo "Notification Function: https://$NOTIFICATION_FUNCTION.azurewebsites.net"
echo ""

# Build parameters with resource names
PARAMS="location=$LOCATION"
PARAMS="$PARAMS sqlAdminPassword=$SQL_ADMIN_PASSWORD"
PARAMS="$PARAMS vnetName=$VNET_NAME"
PARAMS="$PARAMS appSubnetName=$APP_SUBNET_NAME"
PARAMS="$PARAMS funcSubnetName=$FUNC_SUBNET_NAME"
PARAMS="$PARAMS appServicePlanName=$APP_SERVICE_PLAN_NAME"
PARAMS="$PARAMS functionAppServicePlanName=$FUNCTION_APP_SERVICE_PLAN_NAME"
PARAMS="$PARAMS sqlDbName=$SQL_DB_NAME"
PARAMS="$PARAMS storageAccountName=$STORAGE_NAME"
PARAMS="$PARAMS sqlServerName=$SQL_SERVER_NAME"
PARAMS="$PARAMS serviceBusNamespaceName=$SERVICEBUS_NAME"
PARAMS="$PARAMS appInsightsName=$APPINSIGHTS_NAME"
PARAMS="$PARAMS logAnalyticsWorkspaceName=$LOG_ANALYTICS_NAME"
PARAMS="$PARAMS insuranceManagementAppName=$INSURANCE_MGMT_APP"
PARAMS="$PARAMS vehicleInsuranceAppName=$VEHICLE_INSURANCE_APP"
PARAMS="$PARAMS billingServiceFunctionName=$BILLING_FUNCTION"
PARAMS="$PARAMS notificationServiceFunctionName=$NOTIFICATION_FUNCTION"


echo "Deploying infrastructure..."
DEPLOYMENT_OUTPUT=$(az deployment group create \
  --resource-group "$RG" \
  --template-file insurance-infra.bicep \
  --parameters $PARAMS \
  --query 'properties.outputs' \
  --output json)

echo ""
echo "Deployment complete!"
echo ""

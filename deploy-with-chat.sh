#!/bin/bash
# deploy-with-chat.sh - Full deployment script including GenAI services for Chat UI
# This script deploys everything including Azure OpenAI and AI Search
#
# Prerequisites:
# - Azure CLI installed and logged in (az login)
# - Subscription context set
#
# Usage: ./deploy-with-chat.sh

set -e

echo "=========================================="
echo "Expense Management System - Full Deployment with Chat"
echo "=========================================="

# Configuration
RESOURCE_GROUP="rg-expensemgmt-demo"
LOCATION="uksouth"
BASE_NAME="expensemgmt"

# Get current user details for SQL Admin
echo "Getting current user details..."
ADMIN_OBJECT_ID=$(az ad signed-in-user show --query id -o tsv)
ADMIN_LOGIN=$(az ad signed-in-user show --query userPrincipalName -o tsv)

echo "Admin Object ID: $ADMIN_OBJECT_ID"
echo "Admin Login: $ADMIN_LOGIN"

# Create Resource Group if it doesn't exist
echo ""
echo "Step 1: Creating Resource Group..."
az group create --name $RESOURCE_GROUP --location $LOCATION --output none
echo "✓ Resource Group created: $RESOURCE_GROUP"

# Deploy Bicep infrastructure with GenAI
echo ""
echo "Step 2: Deploying infrastructure (App Service, SQL, and GenAI services)..."
DEPLOYMENT_OUTPUT=$(az deployment group create \
    --resource-group $RESOURCE_GROUP \
    --template-file infrastructure/main.bicep \
    --parameters location=$LOCATION \
    --parameters baseName=$BASE_NAME \
    --parameters adminObjectId=$ADMIN_OBJECT_ID \
    --parameters adminLogin=$ADMIN_LOGIN \
    --parameters deployGenAI=true \
    --query properties.outputs -o json)

# Extract outputs
APP_SERVICE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.appServiceName.value')
APP_SERVICE_URL=$(echo $DEPLOYMENT_OUTPUT | jq -r '.appServiceUrl.value')
SQL_SERVER_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.sqlServerName.value')
SQL_SERVER_FQDN=$(echo $DEPLOYMENT_OUTPUT | jq -r '.sqlServerFqdn.value')
DATABASE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.databaseName.value')
MANAGED_IDENTITY_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.managedIdentityName.value')
MANAGED_IDENTITY_CLIENT_ID=$(echo $DEPLOYMENT_OUTPUT | jq -r '.managedIdentityClientId.value')
OPENAI_ENDPOINT=$(echo $DEPLOYMENT_OUTPUT | jq -r '.openAIEndpoint.value')
OPENAI_MODEL_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.openAIModelName.value')
SEARCH_ENDPOINT=$(echo $DEPLOYMENT_OUTPUT | jq -r '.searchEndpoint.value')

echo "✓ Infrastructure deployed"
echo "  App Service: $APP_SERVICE_NAME"
echo "  SQL Server: $SQL_SERVER_FQDN"
echo "  Database: $DATABASE_NAME"
echo "  Managed Identity: $MANAGED_IDENTITY_NAME"
echo "  OpenAI Endpoint: $OPENAI_ENDPOINT"
echo "  OpenAI Model: $OPENAI_MODEL_NAME"
echo "  Search Endpoint: $SEARCH_ENDPOINT"

# Configure App Service settings including GenAI
echo ""
echo "Step 3: Configuring App Service settings..."
az webapp config appsettings set \
    --name $APP_SERVICE_NAME \
    --resource-group $RESOURCE_GROUP \
    --settings \
    "ConnectionStrings__DefaultConnection=Server=tcp:${SQL_SERVER_FQDN},1433;Database=${DATABASE_NAME};Authentication=Active Directory Managed Identity;User Id=${MANAGED_IDENTITY_CLIENT_ID};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;" \
    "OpenAI__Endpoint=${OPENAI_ENDPOINT}" \
    "OpenAI__DeploymentName=${OPENAI_MODEL_NAME}" \
    "Search__Endpoint=${SEARCH_ENDPOINT}" \
    "GenAI__Enabled=true" \
    --output none
echo "✓ App Service settings configured"

# Wait for SQL Server to be fully ready
echo ""
echo "Step 4: Waiting 30 seconds for SQL Server to be fully ready..."
sleep 30
echo "✓ Wait completed"

# Add current IP to SQL firewall
echo ""
echo "Step 5: Adding current IP to SQL firewall..."
CURRENT_IP=$(curl -s https://api.ipify.org)
az sql server firewall-rule create \
    --resource-group $RESOURCE_GROUP \
    --server $SQL_SERVER_NAME \
    --name "DeploymentMachine" \
    --start-ip-address $CURRENT_IP \
    --end-ip-address $CURRENT_IP \
    --output none 2>/dev/null || echo "Firewall rule may already exist"
echo "✓ Firewall rule added for IP: $CURRENT_IP"

# Install Python dependencies
echo ""
echo "Step 6: Installing Python dependencies..."
pip3 install --quiet pyodbc azure-identity
echo "✓ Python dependencies installed"

# Update Python scripts with actual values
echo ""
echo "Step 7: Updating Python scripts with deployment values..."

# Cross-platform sed (works on Mac and Linux)
sed -i.bak "s/example.database.windows.net/${SQL_SERVER_FQDN}/g" run-sql.py && rm -f run-sql.py.bak
sed -i.bak "s/database_name/${DATABASE_NAME}/g" run-sql.py && rm -f run-sql.py.bak

sed -i.bak "s/example.database.windows.net/${SQL_SERVER_FQDN}/g" run-sql-dbrole.py && rm -f run-sql-dbrole.py.bak
sed -i.bak "s/database_name/${DATABASE_NAME}/g" run-sql-dbrole.py && rm -f run-sql-dbrole.py.bak
sed -i.bak "s/MANAGED-IDENTITY-NAME/${MANAGED_IDENTITY_NAME}/g" script.sql && rm -f script.sql.bak

sed -i.bak "s/example.database.windows.net/${SQL_SERVER_FQDN}/g" run-sql-stored-procs.py && rm -f run-sql-stored-procs.py.bak
sed -i.bak "s/database_name/${DATABASE_NAME}/g" run-sql-stored-procs.py && rm -f run-sql-stored-procs.py.bak

echo "✓ Python scripts updated"

# Import database schema
echo ""
echo "Step 8: Importing database schema..."
python3 run-sql.py
echo "✓ Database schema imported"

# Configure database roles for managed identity
echo ""
echo "Step 9: Configuring database roles for managed identity..."
python3 run-sql-dbrole.py
echo "✓ Database roles configured"

# Create stored procedures
echo ""
echo "Step 10: Creating stored procedures..."
python3 run-sql-stored-procs.py
echo "✓ Stored procedures created"

# Deploy application code
echo ""
echo "Step 11: Deploying application code..."
az webapp deploy \
    --resource-group $RESOURCE_GROUP \
    --name $APP_SERVICE_NAME \
    --src-path ./app.zip \
    --type zip
echo "✓ Application deployed"

echo ""
echo "=========================================="
echo "Full Deployment Complete!"
echo "=========================================="
echo ""
echo "App URL: ${APP_SERVICE_URL}/Index"
echo "Chat UI: ${APP_SERVICE_URL}/Chat"
echo ""
echo "Note: Navigate to ${APP_SERVICE_URL}/Index to view the application"
echo "      The Chat UI is available at ${APP_SERVICE_URL}/Chat"
echo ""
echo "To run locally, update appsettings.json connection string to use:"
echo "  Authentication=Active Directory Default"
echo "Then run: az login && dotnet run"
echo ""

# setup-github-azure-oidc.ps1 - Configure Azure OIDC authentication for GitHub Actions
# This script sets up the Azure AD App Registration and Service Principal needed
# for GitHub Actions to authenticate to Azure without storing credentials.
#
# Prerequisites:
# - Azure CLI installed and logged in (az login)
# - Contributor access to the subscription
#
# Usage: .\setup-github-azure-oidc.ps1

$ErrorActionPreference = "Continue"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "GitHub Actions Azure OIDC Setup" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# Configuration
$APP_NAME = "gh-expensemgmt-deployer"
$gitRemote = git remote get-url origin
$GITHUB_REPO = $gitRemote -replace '^https://github.com/', '' -replace '\.git$', ''

# Get current subscription
Write-Host ""
Write-Host "Step 1: Getting subscription details..." -ForegroundColor Yellow
$SUBSCRIPTION_ID = az account show --query id -o tsv
$TENANT_ID = az account show --query tenantId -o tsv

Write-Host "✓ Subscription ID: $SUBSCRIPTION_ID" -ForegroundColor Green
Write-Host "✓ Tenant ID: $TENANT_ID" -ForegroundColor Green

# Create Azure AD App Registration
Write-Host ""
Write-Host "Step 2: Creating Azure AD App Registration..." -ForegroundColor Yellow
az ad app create --display-name $APP_NAME --output none 2>$null
if ($LASTEXITCODE -ne 0) { Write-Host "App may already exist" -ForegroundColor Gray }

$APP_ID = az ad app list --display-name $APP_NAME --query [0].appId -o tsv
Write-Host "✓ App ID (Client ID): $APP_ID" -ForegroundColor Green

# Create Service Principal
Write-Host ""
Write-Host "Step 3: Creating Service Principal..." -ForegroundColor Yellow
az ad sp create --id $APP_ID --output none 2>$null
if ($LASTEXITCODE -ne 0) { Write-Host "Service Principal may already exist" -ForegroundColor Gray }

$OBJECT_ID = az ad sp list --display-name $APP_NAME --query [0].id -o tsv
Write-Host "✓ Service Principal Object ID: $OBJECT_ID" -ForegroundColor Green

# Add federated credential for main branch
Write-Host ""
Write-Host "Step 4: Adding federated credential for main branch..." -ForegroundColor Yellow

# Create JSON file with proper formatting
$subject = "repo:$GITHUB_REPO`:ref:refs/heads/main"
@{
    name = "gh-expensemgmt-main"
    issuer = "https://token.actions.githubusercontent.com"
    subject = $subject
    audiences = @("api://AzureADTokenExchange")
} | ConvertTo-Json | Out-File -FilePath "fedcred.json" -Encoding utf8

# Delete existing credentials if they exist
az ad app federated-credential delete --id $APP_ID --federated-credential-id gh-expensemgmt-all-branches 2>$null
az ad app federated-credential delete --id $APP_ID --federated-credential-id gh-expensemgmt-main 2>$null

# Create new credential
az ad app federated-credential create --id $APP_ID --parameters fedcred.json

# Cleanup
Remove-Item -Path "fedcred.json" -Force

Write-Host "✓ Federated credential created for repo: $GITHUB_REPO (main branch)" -ForegroundColor Green

# Assign Contributor role
Write-Host ""
Write-Host "Step 5: Assigning Contributor role..." -ForegroundColor Yellow
az role assignment create `
  --assignee $OBJECT_ID `
  --role Contributor `
  --scope "/subscriptions/$SUBSCRIPTION_ID" `
  --output none 2>$null
if ($LASTEXITCODE -ne 0) { Write-Host "Role assignment may already exist" -ForegroundColor Gray }

Write-Host "✓ Contributor role assigned" -ForegroundColor Green

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Setup Complete!" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Add these secrets to GitHub:" -ForegroundColor Yellow
Write-Host "  Repository: https://github.com/${GITHUB_REPO}/settings/secrets/actions" -ForegroundColor Gray
Write-Host ""
Write-Host "AZURE_CLIENT_ID=$APP_ID" -ForegroundColor White
Write-Host "AZURE_TENANT_ID=$TENANT_ID" -ForegroundColor White
Write-Host "AZURE_SUBSCRIPTION_ID=$SUBSCRIPTION_ID" -ForegroundColor White
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Go to GitHub repository settings" -ForegroundColor Gray
Write-Host "2. Navigate to: Settings → Secrets and variables → Actions" -ForegroundColor Gray
Write-Host "3. Click 'New repository secret' and add each secret above" -ForegroundColor Gray
Write-Host "4. Go to Actions tab and run the 'Deploy Expense Management System' workflow" -ForegroundColor Gray
Write-Host ""

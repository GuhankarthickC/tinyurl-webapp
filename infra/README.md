# TinyURL Infrastructure as Code (Terraform)

This folder contains Terraform configuration to provision all Azure resources for the TinyURL application.

## Prerequisites

1. **Install Terraform:**
   ```powershell
   winget install HashiCorp.Terraform
   ```

2. **Install Azure CLI:**
   ```powershell
   winget install Microsoft.AzureCLI
   ```

3. **Login to Azure:**
   ```powershell
   az login
   ```

4. **Get your subscription ID:**
   ```powershell
   az account show --query id -o tsv
   ```

## Setup

1. **Copy the example variables file:**
   ```powershell
   Copy-Item terraform.tfvars.example terraform.tfvars
   ```

2. **Edit `terraform.tfvars`** and fill in your values:
   - `subscription_id` - Your Azure subscription ID
   - `sql_admin_password` - Strong password for SQL Server
   - Other settings as needed

## Resources Created

This Terraform configuration provisions:

- ✓ **Resource Group** - Container for all resources
- ✓ **Azure SQL Server** - Database server
- ✓ **Azure SQL Database** - TinyUrlDb database
- ✓ **Storage Account** - For blobs and function storage
- ✓ **Blob Containers** - logs and frontend
- ✓ **App Service Plan (Linux)** - For backend API
- ✓ **Linux Web App** - Backend .NET 9 API
- ✓ **App Service Plan (Consumption)** - For Azure Functions
- ✓ **Linux Function App** - Cleanup function
- ✓ **Static Web App** - Angular frontend hosting
- ✓ **Application Insights** - Monitoring and logging

## Usage

### Initialize Terraform
```powershell
terraform init
```

### Preview changes
```powershell
terraform plan
```

### Apply (create resources)
```powershell
terraform apply
```

### Show outputs
```powershell
terraform output
```

### Destroy all resources
```powershell
terraform destroy
```

## Outputs

After applying, you'll get:
- Backend API URL
- Frontend URL
- Function App URL
- SQL Server details
- Storage Account details
- Application Insights keys

## Important Notes

- **terraform.tfvars** is gitignored - never commit passwords!
- Default SKUs are cost-optimized (B1 for apps, Basic for SQL)
- Modify `variables.tf` defaults for different configurations
- Static Web App is in Central US (limited region availability)

## Cost Details

**Paid Resources:**
- App Service (B1): ~$13/month
- SQL Database (Basic): ~$5/month
- Function App (Consumption): Pay per execution (~$0-2/month for light usage)
- Storage Account: ~$0.50/month

**Free Resources:**
- Static Web App: Free tier ✓
- Application Insights: Free tier (5GB/month) ✓

**Estimated monthly cost: ~$18-20 USD**

**Note:** Static Web Apps are only available in certain regions (Central US, East US 2, West Europe, East Asia). The configuration uses **East Asia**.
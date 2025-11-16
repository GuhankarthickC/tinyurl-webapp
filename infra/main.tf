# ============================================================================
# TinyURL Infrastructure as Code
# ============================================================================
# IMPORTANT: These resources already exist in Azure!
# This Terraform configuration documents the existing infrastructure.
# 
# To use with existing resources, you need to import them first:
#   terraform import azurerm_resource_group.main /subscriptions/{id}/resourceGroups/tinyurl-rg
#   terraform import azurerm_mssql_server.main /subscriptions/{id}/resourceGroups/tinyurl-rg/providers/Microsoft.Sql/servers/tinyurl-sqlserver
#   ... (and so on for each resource)
#
# OR create new resources in a different environment by changing resource names.
# ============================================================================

# Resource Group
resource "azurerm_resource_group" "main" {
  name     = var.resource_group_name
  location = var.location
  tags     = var.tags
}

# Storage Account for Blob Storage and Function App
resource "azurerm_storage_account" "main" {
  name                     = "tinyurlstorageprod"
  resource_group_name      = azurerm_resource_group.main.name
  location                 = azurerm_resource_group.main.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  
  blob_properties {
    versioning_enabled = true
  }
  
  tags = var.tags
}

# Blob Container for logs
resource "azurerm_storage_container" "logs" {
  name                  = "logs"
  storage_account_id    = azurerm_storage_account.main.id
  container_access_type = "private"
}

# Blob Container for frontend static website
resource "azurerm_storage_container" "frontend" {
  name                  = "frontend"
  storage_account_id    = azurerm_storage_account.main.id
  container_access_type = "blob"
}

# SQL Server
resource "azurerm_mssql_server" "main" {
  name                         = "tinyurl-sqlserver"
  resource_group_name          = azurerm_resource_group.main.name
  location                     = azurerm_resource_group.main.location
  version                      = "12.0"
  administrator_login          = var.sql_admin_username
  administrator_login_password = var.sql_admin_password
  minimum_tls_version          = "1.2"
  
  tags = var.tags
}

# SQL Database
resource "azurerm_mssql_database" "main" {
  name           = var.sql_database_name
  server_id      = azurerm_mssql_server.main.id
  collation      = "SQL_Latin1_General_CP1_CI_AS"
  max_size_gb    = 2
  sku_name       = "Basic"
  zone_redundant = false
  
  tags = var.tags
}

# Firewall rule to allow Azure services
resource "azurerm_mssql_firewall_rule" "allow_azure_services" {
  name             = "AllowAzureServices"
  server_id        = azurerm_mssql_server.main.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

# App Service Plan for Backend API
resource "azurerm_service_plan" "backend" {
  name                = "ASP-tinyurlrg-8b8e"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  os_type             = "Linux"
  sku_name            = var.app_service_sku
  
  tags = var.tags
}

# Backend API Web App
resource "azurerm_linux_web_app" "backend" {
  name                = "tinyurl-api-in"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_service_plan.backend.location
  service_plan_id     = azurerm_service_plan.backend.id
  
  site_config {
    always_on = false
    
    application_stack {
      dotnet_version = "9.0"
    }
    
    cors {
      allowed_origins = ["*"]
    }
  }
  
  app_settings = {
    "ASPNETCORE_ENVIRONMENT"                   = var.environment
    "AppSettings__BaseUrl"                     = "https://tinyurl-api-in.azurewebsites.net"
    "WEBSITE_RUN_FROM_PACKAGE"                 = "1"
  }
  
  connection_string {
    name  = "DefaultConnection"
    type  = "SQLAzure"
    value = "Server=tcp:${azurerm_mssql_server.main.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_mssql_database.main.name};Persist Security Info=False;User ID=${var.sql_admin_username};Password=${var.sql_admin_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  }
  
  logs {
    application_logs {
      file_system_level = "Information"
    }
    
    http_logs {
      file_system {
        retention_in_days = 7
        retention_in_mb   = 35
      }
    }
  }
  
  tags = var.tags
}

# App Service Plan for Function App
resource "azurerm_service_plan" "functions" {
  name                = "tinyurl-functions-plan"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  os_type             = "Linux"
  sku_name            = var.function_app_sku # Y1 = Consumption plan
  
  tags = var.tags
}

# Function App for cleanup jobs
resource "azurerm_linux_function_app" "cleanup" {
  name                       = "tinyurl-cleanup-func"
  resource_group_name        = azurerm_resource_group.main.name
  location                   = azurerm_resource_group.main.location
  service_plan_id            = azurerm_service_plan.functions.id
  storage_account_name       = azurerm_storage_account.main.name
  storage_account_access_key = azurerm_storage_account.main.primary_access_key
  
  site_config {
    application_stack {
      dotnet_version              = "9.0"
      use_dotnet_isolated_runtime = true
    }
  }
  
  app_settings = {
    "FUNCTIONS_WORKER_RUNTIME"       = "dotnet-isolated"
    "WEBSITE_RUN_FROM_PACKAGE"       = "1"
    "AzureWebJobsDisableHomepage"    = "true"
  }
  
  connection_string {
    name  = "DefaultConnection"
    type  = "SQLAzure"
    value = "Server=tcp:${azurerm_mssql_server.main.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_mssql_database.main.name};Persist Security Info=False;User ID=${var.sql_admin_username};Password=${var.sql_admin_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  }
  
  tags = var.tags
}

# Static Web App for Angular Frontend
resource "azurerm_static_web_app" "frontend" {
  name                = "tinyurl-frontend"
  resource_group_name = azurerm_resource_group.main.name
  location            = "East Asia" 
  sku_tier            = "Free"
  sku_size            = "Free"
  
  tags = var.tags
}

# Application Insights for monitoring
resource "azurerm_application_insights" "main" {
  name                = "tinyurl-insights"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  application_type    = "web"
  
  tags = var.tags
}

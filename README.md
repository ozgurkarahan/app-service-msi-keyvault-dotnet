---
services: app-service, key-vault, redis
platforms: dotnet
modified by: ozkara-msft
---


# Use Key Vault from App Service with Managed Service Identity to Create Redis Connection String

## Background
For Service-to-Azure-Service authentication, the approach so far involved creating an Azure AD application and associated credential, and using that credential to get a token. The sample [here](https://docs.microsoft.com/en-us/azure/key-vault/key-vault-use-from-web-application) shows how this approach is used to authenticate to Azure Key Vault from a Web App. While this approach works well, there are two shortcomings:
1. The Azure AD application credentials are typically hard coded in source code. Developers tend to push the code to source repositories as-is, which leads to credentials in source.
2. The Azure AD application credentials expire, and so need to be renewed, else can lead to application downtime.

With [Managed Service Identity (MSI)](https://docs.microsoft.com/en-us/azure/app-service/app-service-managed-service-identity), both these problems are solved. This sample shows how a Web App can authenticate to Azure Key Vault without the need to explicitly create an Azure AD application or manage its credentials. 

>Here's another sample that shows how to programatically deploy an ARM template from a .NET Console application running on an Azure VM with a Managed Service Identity (MSI) - [https://github.com/Azure-Samples/windowsvm-msi-arm-dotnet](https://github.com/Azure-Samples/windowsvm-msi-arm-dotnet)

## Prerequisites
To run and deploy this sample, you need the following:
1. An Azure subscription to create an App Service and a Key Vault. 
2. [Azure CLI 2.0](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest) to run the application on your local development machine.

## Step 1: Create an App Service with a Managed Service Identity (MSI)
<a href="https://portal.azure.com/#create/Microsoft.Template/uri/https://raw.githubusercontent.com/ozgurkarahan/app-service-msi-keyvault-dotnet/master/ArmTemplates/webapptemplate.json" target="_blank">
    <img src="http://azuredeploy.net/deploybutton.png"/>
</a>

Use the "Deploy to Azure" button to deploy an ARM template to create the following resources:
1. App Service with MSI.

## Step 2: Create a Redis Cache Instance 
<a href="https://portal.azure.com/#create/Microsoft.Template/uri/https://raw.githubusercontent.com/ozgurkarahan/app-service-msi-keyvault-dotnet/master/ArmTemplates/redisdeploytemplate.json" target="_blank">
    <img src="http://azuredeploy.net/deploybutton.png"/>
</a>
1. Azure Redis Cache Instance.

## Step 3: Create a Redis Cache Instance 
<a href="https://portal.azure.com/#create/Microsoft.Template/uri/https://raw.githubusercontent.com/ozgurkarahan/app-service-msi-keyvault-dotnet/master/ArmTemplates/redisdeploytemplate.json" target="_blank">
    <img src="http://azuredeploy.net/deploybutton.png"/>
</a>
1. Key Vault with a secret (redis cache primary key), and an access policy that grants the App Service access to **Get Secrets**.
>Note: The template will get the redisCacheName and webAppName as parameter. From redis cache intance, it will get the primarykey and will add it as a secret with the keyVaultSecretName. So then the uri to access this key will be "https://#{keyVaultName}#.vault.azure.net/secrets/#{keyVaultSecretName}#"

Review the resources created using the Azure portal. You should see an App Service and a Key Vault. View the access policies of the Key Vault to see that the App Service has access to it. 

## Step 3: Clone the code on your local environement. 


The project has two relevant Nuget packages:
1. Microsoft.Azure.Services.AppAuthentication (preview) - makes it easy to fetch access tokens for Service-to-Azure-Service authentication scenarios. 
2. Microsoft.Azure.KeyVault - contains methods for interacting with Key Vault. 

The relevant code is in WebAppKeyVault/WebAppKeyVault/Controllers/HomeController.cs file. The **AzureServiceTokenProvider** class (which is part of Microsoft.Azure.Services.AppAuthentication) tries the following methods to get an access token:-
1. Managed Service Identity (MSI) - for scenarios where the code is deployed to Azure, and the Azure resource supports MSI. 

```csharp    
 public async System.Threading.Tasks.Task<ActionResult> Index()
        {
            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            var redisUri = ConfigurationManager.AppSettings["redisPrimaryKeySecretUri"];
            var redisCacheName = ConfigurationManager.AppSettings["redisCacheName"];
            
            try
            {
                var keyVaultClient = new KeyVaultClient(
                    new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));

                var secret = await keyVaultClient.GetSecretAsync(redisUri).ConfigureAwait(false);
                ViewBag.Secret = $"Secret: {secret.Value}";
                RedisConnectionString = string.Format(ConfigurationManager.AppSettings["redisConnectionString"], redisCacheName, secret.Value);

                ViewBag.RedisConnectionString = $"RedisConnectionString: {RedisConnectionString}";
            }
            catch (Exception exp)
            {
                ViewBag.Error = $"Something went wrong: {exp.Message}";
            }

            ViewBag.Principal = azureServiceTokenProvider.PrincipalUsed != null ? $"Principal Used: {azureServiceTokenProvider.PrincipalUsed}" : string.Empty;

            return View();
        }
```


## Step 6: Deploy the Web App to Azure
Use any of the methods outlined on [Deploy your app to Azure App Service](https://docs.microsoft.com/en-us/azure/app-service-web/web-sites-deploy) to publish the Web App to Azure. 
After you deploy it, browse to the web app. You should see the secret on the web page, and this time the Principal Used will show "App", since it ran under the context of the App Service. 
The AppId of the MSI will be displayed. 

## Summary
The web app was successfully able to get a secret at runtime from Azure Key Vault using your developer account during development, and using MSI when deployed to Azure, without any code change between local development environment and Azure. 
As a result, you did not have to explicitly handle a service principal credential to authenticate to Azure AD to get a token to call Key Vault. You do not have to worry about renewing the service principal credential either, since MSI takes care of that.  


The original repo is: 
https://github.com/Azure-Samples/app-service-msi-keyvault-dotnet

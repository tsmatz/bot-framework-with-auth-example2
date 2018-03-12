This solution is a super super simple bot sample code which is integrated with the OAuth authentication.
Note that, because we want to show you the core logic (the code which shows how to integrate the authentication), this sample doesn't implement the code for real prodction (e.g, security code, exception handling, etc) and uses temporary In-memory data store for bot state store.
Please add these code and implement dedicated custom state store (e.g, state store using Azure Table Storage, CosmosDB, etc) for your production. (Please see the source code comment.)

For this usage, please see my blog post :
https://blogs.msdn.microsoft.com/tsmatsuz/2016/09/06/microsoft-bot-framework-bot-with-authentication-and-signin-login/

Thanks,

///// Step for the setup

1.Register your bot (Bot Channel Registraiton) in Azure Bot Service.
  - Copy your bot id (bot handle), app id, and app password (secret).
  - Set https://{your demo bot}/api/messages as messaging endpoint (webhook url).

2.Fill your "BotId", "MicrosoftAppId", and "MicrosoftAppPassword" in AuthDemoBot2\Web.config.

3.Login to Azure Portal (https://portal.azure.com/) with Office 365 administrator account. (You need your organization account.)
  Go to Azure Active Directory (Azure AD) management, and register your application.
  - Application type must be "Web app / API"
  - Copy app id (client id)
  - Create key (client secret) and copy that value
  - Register "https://{your demo bot}/*" as Reply URLs.
  - Set "Read user mail" (Mail.Read) in "Office 365 Exchange Online" as required permissions
  - Select "Yes" on "Multi-tenanted" setting

5.Fill previous Azure AD app's "ClientId" and "ClientSecret" in AuthDemoWeb\Web.config.

6.Fill https://{your demo bot}/api/callback as "OAuthCallbackUrl" in AuthDemoBot\Web.config.

7.Host your bot (https://{your demo bot}) in public internet.

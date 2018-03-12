using System;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Autofac;
using Microsoft.Bot.Builder.ConnectorEx;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Connector;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace AuthDemoBot2
{
    public class CallbackController : ApiController
    {
        [HttpGet]
        [Route("api/callback")]
        public async Task<HttpResponseMessage> Callback(
            [FromUri] string code,
            [FromUri] string state,
            [FromUri] string session_state = null)
        {
            // Get access token from auth code
            var authContext = new AuthenticationContext("https://login.microsoftonline.com/common");
            var authResult = await authContext.AcquireTokenByAuthorizationCodeAsync(
                code,
                new Uri(ConfigurationManager.AppSettings["OAuthCallbackUrl"]),
                new ClientCredential(
                    ConfigurationManager.AppSettings["ClientId"],
                    ConfigurationManager.AppSettings["ClientSecret"]));

            // Resume conversation
            var stateCol = System.Web.HttpUtility.ParseQueryString(state);
            var address = new Microsoft.Bot.Builder.Dialogs.Address
                (
                    botId: stateCol["botId"],
                    channelId: stateCol["channelId"],
                    userId: stateCol["userId"],
                    conversationId: stateCol["conversationId"],
                    serviceUrl: stateCol["serviceUrl"]
                );
            var convRef = address.ToConversationReference();
            var msg = convRef.GetPostToBotMessage();
            msg.Text = "login_succeed";
            await Conversation.ResumeAsync(convRef, msg);

            // Attach to dialog scope from bot id, service url, ...
            //    and save access token as user state
            using (var scope = DialogModule.BeginLifetimeScope(Conversation.Container, msg))
            {
                var dataStore = scope.Resolve<IBotDataStore<BotData>>();
                var userData = await dataStore.LoadAsync(
                    address,
                    BotStoreType.BotUserData,
                    CancellationToken.None);
                userData.SetProperty("AccessToken", authResult.AccessToken);
                await dataStore.SaveAsync(
                    address,
                    BotStoreType.BotUserData,
                    userData,
                    CancellationToken.None);
                await dataStore.FlushAsync(address, CancellationToken.None);
            }

            return Request.CreateResponse(
                "Succeed ! You can close this window !");
        }
    }
}
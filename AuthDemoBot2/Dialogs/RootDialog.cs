using System;
using System.Threading;
using System.Configuration;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.ConnectorEx;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Autofac;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;

namespace AuthDemoBot2.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);

            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as Activity;

            //// calculate something for us to return
            //int length = (activity.Text ?? string.Empty).Length;

            //// return our reply to the user
            //await context.PostAsync($"You sent {activity.Text} which was {length} characters");

            if (activity.Text == "login")
            {
                // create state (passing data) for OAuth
                var convRef = context.Activity.ToConversationReference();
                var stateCol = System.Web.HttpUtility.ParseQueryString(string.Empty);
                stateCol["userId"] = convRef.User.Id;
                stateCol["botId"] = convRef.Bot.Id;
                stateCol["conversationId"] = convRef.Conversation.Id;
                stateCol["serviceUrl"] = convRef.ServiceUrl;
                stateCol["channelId"] = convRef.ChannelId;

                //var uriBuilder = new UriBuilder(ConfigurationManager.AppSettings["OAuthCallbackUrl"]);
                //var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
                //query["userId"] = convRef.User.Id;
                //query["botId"] = convRef.Bot.Id;
                //query["conversationId"] = convRef.Conversation.Id;
                //query["serviceUrl"] = convRef.ServiceUrl;
                //query["channelId"] = convRef.ChannelId;
                //uriBuilder.Query = query.ToString();
                //var redirectUrl = uriBuilder.ToString();

                // create Azure AD signin context
                var authContext = new AuthenticationContext("https://login.microsoftonline.com/common");
                var authUri = authContext.GetAuthorizationRequestUrlAsync(
                    "https://outlook.office365.com/",
                    ConfigurationManager.AppSettings["ClientId"],
                    new Uri(ConfigurationManager.AppSettings["OAuthCallbackUrl"]),
                    UserIdentifier.AnyUser,
                    "&state=" + System.Web.HttpUtility.UrlEncode(stateCol.ToString()));

                // show SignIn card (setting oauth sign-in url to SignIn card)
                var reply = context.MakeMessage();
                reply.Text = "Authentication Required";
                reply.Attachments.Add(SigninCard.Create(
                    "Login",
                    "Please login to Office 365",
                    authUri.Result.ToString()).ToAttachment());
                await context.PostAsync(reply);
            }
            else if (activity.Text == "get mail")
            {
                string accessToken = string.Empty;

                using (var scope = DialogModule.BeginLifetimeScope(Conversation.Container, activity))
                {
                    var dataStore = scope.Resolve<IBotDataStore<BotData>>();
                    var convRef = context.Activity.ToConversationReference();
                    var address = new Microsoft.Bot.Builder.Dialogs.Address
                        (
                            botId: convRef.Bot.Id,
                            channelId: convRef.ChannelId,
                            userId: convRef.User.Id,
                            conversationId: convRef.Conversation.Id,
                            serviceUrl: convRef.ServiceUrl
                        );
                    var userData = await dataStore.LoadAsync(
                        address,
                        BotStoreType.BotUserData,
                        CancellationToken.None);
                    accessToken = userData.GetProperty<string>("AccessToken");
                }

                if(string.IsNullOrEmpty(accessToken))
                {
                    await context.PostAsync("Not logging-in (type \"login\")");
                }
                else
                {
                    // Get recent 10 e-mail from Office 365
                    HttpClient cl = new HttpClient();
                    var acceptHeader =
                        new MediaTypeWithQualityHeaderValue("application/json");
                    cl.DefaultRequestHeaders.Accept.Add(acceptHeader);
                    cl.DefaultRequestHeaders.Authorization
                      = new AuthenticationHeaderValue("Bearer", accessToken);
                    HttpResponseMessage httpRes =
                      await cl.GetAsync("https://outlook.office365.com/api/v1.0/me/messages?$orderby=DateTimeSent%20desc&$top=10&$select=Subject,From");
                    if (httpRes.IsSuccessStatusCode)
                    {
                        var strRes = httpRes.Content.ReadAsStringAsync().Result;
                        JObject jRes = await httpRes.Content.ReadAsAsync<JObject>();
                        JArray jValue = (JArray)jRes["value"];
                        foreach (JObject jItem in jValue)
                        {
                            string sub = $"Subject={((JValue)jItem["Subject"]).Value}";
                            sub = sub.Replace('<', ' ').Replace('>', ' ').Replace('[', ' ').Replace(']',' ');
                            await context.PostAsync(sub);
                        }
                    }
                    else
                    {
                        await context.PostAsync("Failed to get e-mail.\n\nPlease type \"login\" before you get e-mail.");
                    }
                }

            }
            else if (activity.Text == "revoke")
            {
                await context.PostAsync("Click [here](https://account.activedirectory.windowsazure.com/), login, and remove this app (Bot with Office 365 Authentication Example).");
            }
            else if (activity.Text == "login_succeed")
            {
                await context.PostAsync("You logged in !");
            }
            else
            {
                await context.PostAsync("# Bot Help\n\nType the following command. (You need your Office 365 Exchange Online subscription.)\n\n**login** -- Login to Office 365\n\n**get mail** -- Get your e-mail from Office 365\n\n**revoke** -- Revoke permissions for accessing your e-mail");
            }

            context.Wait(MessageReceivedAsync);
        }
    }
}
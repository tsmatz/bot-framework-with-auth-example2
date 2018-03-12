using System.Reflection;
using System.Web.Http;
using Autofac;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Connector;

namespace AuthDemoBot2
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            Conversation.UpdateContainer(builder =>
            {
                // Here we use InMemoryDataStore, which is developing purpose and temporary.
                // Please change to the custom data store for your producton. (TableBotDataStore, DocumentDbBotDataStore, etc)
                builder.RegisterModule(new AzureModule(Assembly.GetExecutingAssembly()));
                var store = new InMemoryDataStore();
                builder.Register(c => store)
                                .Keyed<IBotDataStore<BotData>>(AzureModule.Key_DataStore)
                                .AsSelf()
                                .SingleInstance();
            });

            GlobalConfiguration.Configure(WebApiConfig.Register);
        }
    }
}

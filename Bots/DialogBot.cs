// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio CoreBot v4.18.1

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SnkrBot.Dialogs;
using SnkrBot.Models;

namespace SnkrBot.Bots
{
    // This IBot implementation can run any type of Dialog. The use of type parameterization is to allows multiple different bots
    // to be run at different endpoints within the same project. This can be achieved by defining distinct Controller types
    // each with dependency on distinct IBot types, this way ASP Dependency Injection can glue everything together without ambiguity.
    // The ConversationState is used by the Dialog system. The UserState isn't, however, it might have been used in a Dialog implementation,
    // and the requirement is that all BotState objects are saved at the end of a turn.
    public class DialogBot<T> : ActivityHandler
        where T : Dialog
    {
#pragma warning disable SA1401 // Fields should be private
        protected readonly Dialog Dialog;
        protected readonly BotState ConversationState;
        protected readonly BotState UserState;
        protected readonly ILogger Logger;
        private readonly IConfiguration Configuration;
#pragma warning restore SA1401 // Fields should be private

        public DialogBot(ConversationState conversationState, UserState userState, T dialog, ILogger<DialogBot<T>> logger, IConfiguration configuration)
        {
            ConversationState = conversationState;
            UserState = userState;
            Dialog = dialog;
            Logger = logger;
            Configuration = configuration;
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            await base.OnTurnAsync(turnContext, cancellationToken);

            // Save any state changes that might have occurred during the turn.
            await ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await UserState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Running dialog with Message Activity.");

            // Controlla se è un click del bottone (il Value non sarà null)
            if (turnContext.Activity.Value != null)
            {
                try
                {
                    var buttonData = JsonConvert.DeserializeObject<dynamic>(turnContext.Activity.Value.ToString());
                    DateTime releaseDate = ShoeDialog.formatDate(buttonData.release.ToString());

                    var scheduledMessage = new TeamsScheduledMessage
                    {
                        ServiceUrl = buttonData.serviceUrl.ToString(),
                        ConversationId = buttonData.conversationId.ToString(),
                        MessageText = $"Reminder: Il release di {buttonData.shoeName} è tra 1 ora!",
                        ScheduledTime = releaseDate.AddHours(-1)
                    };

                    await SendToServiceBus(scheduledMessage);

                    await turnContext.SendActivityAsync(
                        $"Ho programmato un reminder per {buttonData.shoeName} un'ora prima del release.",
                        cancellationToken: cancellationToken);
                    
                }
                catch (Exception ex)
                {
                    var buttonData = JsonConvert.DeserializeObject<dynamic>(turnContext.Activity.Value.ToString());
                    Logger.LogError($"Errore nella gestione del bottone: {ex.Message}");
                    await turnContext.SendActivityAsync($"release{ShoeDialog.formatDate(buttonData.release.ToString())} \nMi dispiace, c'è stato un errore nella programmazione del reminder.\n {ex.Message}{ex.StackTrace}");
                }
            }
            else
            {
                // Gestione normale dei dialoghi
                await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>("DialogState"), cancellationToken);
            }
        }

        private async Task SendToServiceBus(TeamsScheduledMessage message)
        {
            var connectionString = Configuration["ServiceBusConnectionString"]; 
            var client = new ServiceBusClient(connectionString);
            var sender = client.CreateSender("reminders");

            var messageBody = JsonConvert.SerializeObject(message);
            var serviceBusMessage = new ServiceBusMessage(messageBody)
            {
                ScheduledEnqueueTime = message.ScheduledTime
            };

            await sender.SendMessageAsync(serviceBusMessage);
        }
    }
}

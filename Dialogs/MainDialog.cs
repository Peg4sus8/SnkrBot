using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using SnkrBot.CognitiveModels;

namespace SnkrBot.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private readonly FlightBookingRecognizer _luisRecognizer;
        private readonly ILogger _logger;

        public MainDialog(FlightBookingRecognizer luisRecognizer, ShoeDialog shoeDialog, ILogger<MainDialog> logger)
            : base(nameof(MainDialog))
        {
            _luisRecognizer = luisRecognizer;
            _logger = logger;

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(shoeDialog);

            var waterfallSteps = new WaterfallStep[]
            {
                IntroStepAsync,
                ActStepAsync,
                FinalStepAsync,
            };

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
           /* if (!_luisRecognizer.IsConfigured)
            {
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("NOTE: LUIS is not configured. To enable all capabilities, add 'LuisAppId', 'LuisAPIKey' and 'LuisAPIHostName' to the appsettings.json file.", inputHint: InputHints.IgnoringInput), cancellationToken);

                return await stepContext.NextAsync(null, cancellationToken);
            }*/

            var messageText = stepContext.Options?.ToString() ??
                "Ciao! Sono il tuo assistente per la ricerca di scarpe. Posso aiutarti a:\n" +
                "- Vedere tutte le scarpe disponibili\n" +
                "- Cercare scarpe di un brand specifico (es. 'Mostrami le Nike')\n" +
                "- Cercare scarpe in un range di prezzo (es. 'Scarpe sotto i 200€' o 'Scarpe tra 100€ e 300€')\n" +
                "Come posso aiutarti?";

            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (!_luisRecognizer.IsConfigured)
            {
                return await stepContext.BeginDialogAsync(nameof(ShoeDialog), new FilterDetails(), cancellationToken);
            }

            // Analizza l'input dell'utente per determinare i filtri
            var filterDetails = await ParseUserInput(stepContext.Context, cancellationToken);

            // Avvia il dialogo delle scarpe con i filtri
            return await stepContext.BeginDialogAsync(nameof(ShoeDialog), filterDetails, cancellationToken);
        }

        private async Task<FilterDetails> ParseUserInput(ITurnContext context, CancellationToken cancellationToken)
        {
            var text = context.Activity.Text.ToLower();
            var filterDetails = new FilterDetails();

            // Cerca menzioni di brand
            if (text.Contains("nike")) filterDetails.Brand = "Nike";
            else if (text.Contains("adidas")) filterDetails.Brand = "Adidas";
            else if (text.Contains("jordan")) filterDetails.Brand = "Jordan";
            else if (text.Contains("new balance")) filterDetails.Brand = "New Balance";

            // Cerca menzioni di prezzi
            if (text.Contains("sotto") || text.Contains("meno di"))
            {
                var priceStr = new string(text.Where(c => char.IsDigit(c)).ToArray());
                if (decimal.TryParse(priceStr, out decimal maxPrice))
                {
                    filterDetails.MaxPrice = maxPrice;
                }
            }
            else if (text.Contains("sopra") || text.Contains("più di"))
            {
                var priceStr = new string(text.Where(c => char.IsDigit(c)).ToArray());
                if (decimal.TryParse(priceStr, out decimal minPrice))
                {
                    filterDetails.MinPrice = minPrice;
                }
            }
            else if (text.Contains("tra"))
            {
                var numbers = new string(text.Where(c => char.IsDigit(c) || c == ' ').ToArray())
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Select(n => decimal.TryParse(n, out decimal num) ? num : 0)
                    .Where(n => n > 0)
                    .ToList();

                if (numbers.Count >= 2)
                {
                    filterDetails.MinPrice = numbers[0];
                    filterDetails.MaxPrice = numbers[1];
                }
            }

            return filterDetails;
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var promptMessage = "Posso aiutarti a cercare altre scarpe?";
            return await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);
        }
    }
}
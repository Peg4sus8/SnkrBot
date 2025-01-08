using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SnkrBot.CognitiveModels;
using SnkrBot.Models;
using static System.Net.Mime.MediaTypeNames;

namespace SnkrBot.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private readonly ShoeRecognizer _cluRecognizer;
        private readonly ILogger _logger;

        public MainDialog(ShoeRecognizer luisRecognizer, ShoeDialog shoeDialog, ILogger<MainDialog> logger)
            : base(nameof(MainDialog))
        {
            _cluRecognizer = luisRecognizer;
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
            if (!_cluRecognizer.IsConfigured())
            {
                await stepContext.Context.SendActivityAsync(
    MessageFactory.Text("Errore: il riconoscitore CLU non è configurato correttamente. Non posso procedere con la richiesta."), cancellationToken);

                // Termina il dialogo se il recognizer non è configurato
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }

            var messageText = stepContext.Options?.ToString() ??
                "Ciao! Sono il tuo assistente per la ricerca di scarpe. Posso aiutarti a:\n" +
                "- Vedere tutte le scarpe disponibili\n" +
                "- Cercare scarpe di un brand specifico (es. 'Mostrami le Nike')\n" +
                "- Cercare scarpe in un range di prezzo (es. 'Scarpe sotto i 200€' o 'Scarpe tra 100€ e 300€')\n" +
                "Come posso aiutarti?";

            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
            var text = stepContext.Result?.ToString()?.Trim();
          
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (!_cluRecognizer.IsConfigured())
            {
                await stepContext.Context.SendActivityAsync(
             MessageFactory.Text("Errore: il riconoscitore CLU non è configurato correttamente. Non posso procedere con la richiesta."),cancellationToken);
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }

            try
            {
                // Esegui l'analisi con il Recognizer
                var cluResult = await _cluRecognizer.RecognizeAsync(stepContext.Context, cancellationToken);
                if (cluResult == null)
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("Non ho capito la tua richiesta. Puoi ripetere?"), cancellationToken);
                    return await stepContext.ReplaceDialogAsync(InitialDialogId, null, cancellationToken);
                }

                var (intent, score) = cluResult.TopIntent();
                if (string.IsNullOrEmpty(intent.ToString()))
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("Non ho capito la tua richiesta. Puoi riprovare?"), cancellationToken);
                    return await stepContext.ReplaceDialogAsync(InitialDialogId, null, cancellationToken);
                }

                // Gestisci gli intenti
                switch (intent)
                {
                    case ShoeRecognizerResult.Intent.ShowAllShoes:
                        return await stepContext.BeginDialogAsync(nameof(ShoeDialog), null, cancellationToken);

                    case ShoeRecognizerResult.Intent.FilterByBrand:
                        var brand = cluResult.Entities?.Brand?.FirstOrDefault();
                        if (string.IsNullOrEmpty(brand))
                        {
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Non ho trovato il brand specificato. Puoi riprovare?"), cancellationToken);
                            return await stepContext.ReplaceDialogAsync(InitialDialogId, null, cancellationToken);
                        }
                        return await stepContext.BeginDialogAsync(nameof(ShoeDialog), new { Filter = "brand", Brand = brand }, cancellationToken);

                    case ShoeRecognizerResult.Intent.FilterByPrice:
                        var maxPrice = cluResult.Entities?.Price?.FirstOrDefault();
                        if (maxPrice == null)
                        {
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Non ho trovato il prezzo specificato. Puoi riprovare?"), cancellationToken);
                            return await stepContext.ReplaceDialogAsync(InitialDialogId, null, cancellationToken);
                        }
                        return await stepContext.BeginDialogAsync(nameof(ShoeDialog), new { Filter = "price", MaxPrice = maxPrice }, cancellationToken);

                    default:
                        await stepContext.Context.SendActivityAsync(
                            MessageFactory.Text("Non sono sicuro di cosa intendi. Puoi riprovare?"), cancellationToken);
                        break;

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'analisi con CLU.");
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Si è verificato un errore: {ex.Message}"), cancellationToken);
            }

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userResponse = stepContext.Result?.ToString()?.Trim().ToLower();
            if (new[] { "no", "non ora", "stop", "fine" }.Contains(userResponse))
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Va bene! Se hai bisogno di aiuto, sono qui."), cancellationToken);
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }

            var promptMessage = "Posso aiutarti a cercare altre scarpe?";
            return await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);
        }

    }
}



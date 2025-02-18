using Microsoft.Bot.Builder.Dialogs;
using SnkrBot.Models;
using SnkrBot.Services;
using Newtonsoft.Json.Linq;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Builder;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using AdaptiveCards.Templating;
using System.IO;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Azure.Core;
using Azure.Identity;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using Microsoft.Graph;
using Microsoft.Graph.Communications.Calls;
using Microsoft.Graph.Models;
using Attachment = Microsoft.Bot.Schema.Attachment;
using System.Globalization;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;

namespace SnkrBot.Dialogs
{
    public class ShoeDialog : ComponentDialog
    {
        private readonly ShoeService _shoeService;
        private readonly ILogger<ShoeDialog> _logger;

        public ShoeDialog(ShoeService shoeService, ILogger<ShoeDialog> logger) : base(nameof(ShoeDialog))
        {
            _shoeService = shoeService;
            _logger = logger;

            var waterfallSteps = new WaterfallStep[]
            {
                ShowFilteredShoesAsync
            };

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> ShowFilteredShoesAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            try
            {
                // Cast di stepContext.Options a FilterDetails
                var filterDetails = stepContext.Options as FilterDetails ?? new FilterDetails();
                
                // Usa il servizio per ottenere le scarpe filtrate
                var shoes = await _shoeService.GetFilteredShoesAsync(filterDetails.Brand, filterDetails.MaxPrice);

                if (!shoes.Any())
                { 
                    await stepContext.Context.SendActivityAsync(
                        "Non ho trovato scarpe che corrispondono ai criteri di ricerca specificati.",
                        cancellationToken: cancellationToken);
                }
                else
                {   // card: MARKDOWN
                    /*var attachments = shoes.Select(shoe => CreateSShoeCard(shoe)).ToList();
                    foreach (var attachment in attachments) 
                    {
                        var reply = MessageFactory.Text(attachment);
                        reply.TextFormat = "markdown";
                        await stepContext.Context.SendActivityAsync(reply, cancellationToken);
                    }

                    // Card: ADAPTIVE
                    var attachments = shoes.Select(shoe => CreateSShoeCard(shoe)).ToList();
                    var reply = MessageFactory.Carousel(attachments);
                    await stepContext.Context.SendActivityAsync(reply, cancellationToken);*/

                    // Card: Hero
                    var attachments = shoes.Select(shoe => CreateShoeCard(shoe, stepContext)).ToList();
                    foreach (var attachment in attachments)
                    {
                        var reply = MessageFactory.Attachment(attachment.ToAttachment());
                        await stepContext.Context.SendActivityAsync(reply, cancellationToken);
                    }

                }
            }
            catch (Exception ex)
            {
                await stepContext.Context.SendActivityAsync(
                    $"Mi dispiace, si è verificato un errore durante il recupero delle scarpe. Riprova più tardi.",
                    cancellationToken: cancellationToken);

                _logger.LogError(ex, "Errore durante il recupero delle scarpe");
            }

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
        private Attachment CreateAdaptiveCard(Shoe shoe)
        {
            var templateJson = File.ReadAllText("ShoeCardTemplate.json");
            var template = new AdaptiveCardTemplate(templateJson);
            var data = new
            {
                name = shoe.Name,
                image = shoe.Img ?? "",
                releaseDate = shoe.Release ?? "N/A",
                price = shoe.Price
            };
            var cardJson = template.Expand(data);

            return new Attachment
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JObject.Parse(cardJson)
            };
        }

        private string CreateSShoeCard(Shoe shoe)
        {

            string markdownMessage = $"**{shoe.Name}**\n\n" +
                                     $"![View Image]({shoe.Img})\n\n" +
                                     $"Release Date: {shoe.Release}\n\n" +
                                     $"Price: {shoe.Price} €";

            return markdownMessage;
        }

        private HeroCard CreateShoeCard(Shoe shoe, WaterfallStepContext stepContext)
        {
            var userId = stepContext.Context.Activity.Id;
            if (!string.IsNullOrEmpty(userId) )
            {
                _logger.LogInformation($"UserId rilevato: {userId}");
            }
            else
            {
                _logger.LogError($"UserId NON trovato: {userId}");
            }
           
            var heroCard = new HeroCard
            {
                Title = shoe.Name,
                Subtitle = shoe.Release,
                Text = "Price: " + shoe.Price,
                Images = new List<CardImage> { new CardImage(shoe.Img) },
                Buttons = new List<CardAction> {
                    new CardAction(ActionTypes.PostBack, "Aggiungi al Calendario", value:$"{shoe.Name};{shoe.Release};{userId}")
                   },
            };

            return heroCard;
        }

        // Inizio gestione creazione eventi        
        public static async Task CallPowerAutomateFlow(string messageContent, string sendDate, string userId)
        {
            // La URL generata dal trigger HTTP di Power Automate
            string powerAutomateUrl = "https://prod-58.westeurope.logic.azure.com:443/workflows/5189fd5bdfdd4c6281c3d7571c09504b/triggers/manual/paths/invoke?api-version=2016-06-01";

            // Crea un oggetto JSON con i dati da inviare
            var data = new
            {
                messageContent = messageContent,
                sendDate = formatDate(sendDate),
                userId = userId  
            };

            string json = JsonConvert.SerializeObject(data);

            using (var client = new HttpClient())
            {
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Invio la richiesta HTTP POST al flusso di Power Automate
                var response = await client.PostAsync(powerAutomateUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Flusso attivato correttamente");
                }
                else
                {
                    Console.WriteLine("Errore nell'attivare il flusso");
                }
            }
        }

        public static DateTime? formatDate(string release)
        {
            string[] formati = { "dd MMM yyyy - HH:mm", "dd MMMM yyyy - HH:mm"};
            CultureInfo[] cultures = { new CultureInfo("it-IT"), new CultureInfo("en-US"), new CultureInfo("de-DE") };
            DateTime parsedDate;
            /* 
             * Creo una stringa "risultato
             * rimuovo la prima parte in cui è indicato il giorno (es. "Do. "
            */
            foreach (CultureInfo culture in cultures)
            {
                if (DateTime.TryParseExact(release, formati, culture, DateTimeStyles.None, out parsedDate))
                    { return parsedDate; }
            }
            
            //string formattedDateTime = inputDateTime.ToString("yyyy-MM-ddTHH:mm:ss");
            return null;
        }

        //-----------------
    }    
}
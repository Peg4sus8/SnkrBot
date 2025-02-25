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
using Newtonsoft.Json;
using Attachment = Microsoft.Bot.Schema.Attachment;
using System.Globalization;

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
            // Parsing della data di rilascio
            DateTime releaseDate;
            if (!DateTime.TryParse(shoe.Release, out releaseDate))
            {
                //releaseDate = DateTime.Now.AddDays(7); // Data di fallback
                releaseDate = formatDate(shoe.Release);
            }

            // Calcola la data di fine evento (1 ora dopo l'inizio)
            var endTime = releaseDate.AddHours(1);

            // Formatta le date per il deep link
            
            var startTimeStr = releaseDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            var endTimeStr = endTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");

            // Creazione del deep link per l'evento del calendario
            var subjectEncoded = Uri.EscapeDataString(shoe.Name + " Release");
            var locationEncoded = Uri.EscapeDataString("Online Release");
            var bodyEncoded = Uri.EscapeDataString($"Release delle {shoe.Name} a {shoe.Price}");

            var calendarDeepLink = $"https://teams.microsoft.com/l/meeting/new?subject={subjectEncoded}&startTime={startTimeStr}&endTime={endTimeStr}&location={locationEncoded}&body={bodyEncoded}";

            var heroCard = new HeroCard
            {
                Title = shoe.Name,
                Subtitle = shoe.Release,
                Text = "Price: " + shoe.Price,
                Images = new List<CardImage> { new CardImage(shoe.Img) },
                Buttons = new List<CardAction> {
                    new CardAction(
                        ActionTypes.OpenUrl,
                        "Aggiungi al Calendario",
                        value: calendarDeepLink)
                },
            };

            return heroCard;
        }

        public static DateTime formatDate(string release)
        {
            // Rimuovi i caratteri non necessari
            release = release.Replace(":", " ").Replace(",", "").Trim();

            // Suddividi la stringa in base agli spazi
            string[] words = release.Split(' ');

            if (words.Length < 6)
            {
                throw new ArgumentException("La stringa di rilascio non è nel formato corretto.");
            }

            // Mappa dei mesi in tedesco
            Dictionary<string, string> monthMapping = new Dictionary<string, string>
            {
                { "Gen", "01" }, { "Feb", "02" }, { "März", "03" }, { "Apr", "04" }, { "Mai", "05" },
                { "Juni", "06" }, { "Jun", "06" }, { "Juli", "07" }, { "Jul", "07" }, { "Aug", "08" },
                { "Sep", "09" }, { "Okt", "10" }, { "Nov", "11" }, { "Dez", "12" }
            };

            // Verifica che il mese sia valido e sostituisci con il numero del mese
            if (!monthMapping.ContainsKey(words[1]))
            {
                throw new ArgumentException("Mese non valido: " + words[1]);
            }

            words[1] = monthMapping[words[1]];  // Sostituisce il mese con il numero del mese

            // Creazione della data
            int day = Int32.Parse(words[0]);
            int month = Int32.Parse(words[1]);
            int year = Int32.Parse(words[2]);
            int hour = Int32.Parse(words[4]);
            int minute = Int32.Parse(words[5]);

            // Restituisci la data
            return new DateTime(year, month, day, hour, minute, 0);
        }


        //-----------------
    }
}
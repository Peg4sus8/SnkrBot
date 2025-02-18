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
            var conversationId = stepContext.Context.Activity.Conversation.Id;
            var serviceUrl = stepContext.Context.Activity.ServiceUrl;

            var heroCard = new HeroCard
            {
                Title = shoe.Name,
                Subtitle = shoe.Release,
                Text = "Price: " + shoe.Price,
                Images = new List<CardImage> { new CardImage(shoe.Img) },
                Buttons = new List<CardAction> {
                    new CardAction(ActionTypes.PostBack, 
                        "Aggiungi al Calendario", 
                        value: JsonConvert.SerializeObject(new {
                            shoeName = shoe.Name,
                            release = shoe.Release,
                            conversationId = conversationId,
                            serviceUrl = serviceUrl
                        }))
                   },
            };

            return heroCard;
        }

        // Inizio gestione creazione eventi        
        

        public static DateTime formatDate(string release)
        {
            release.Replace(":", " ");
            string[] words = release.Split(' ');

            switch (words[1])
            {
                case "Gen":
                    words[1] = "01"; break;
                case "Feb":
                    words[1] = "02"; break;
                case "März":
                    words[1] = "03"; break;
                case "Apr" or "April":
                    words[1] = "04"; break;
                case "Mai":
                    words[1] = "05"; break;
                case "Juni" or "Jun":
                    words[1] = "06"; break;
                case "Juli" or "Jul":
                    words[1] = "07"; break;
                case "Aug":
                    words[1] = "08"; break;
                case "Sep":
                    words[1] = "09"; break;
                case "Okt":
                    words[1] = "10"; break;
                case "Nov":
                    words[1] = "11"; break;
                case "Dez":
                    words[1] = "12"; break;
            }           
        
            return new DateTime(Int32.Parse(words[2]), Int32.Parse(words[1]), Int32.Parse(words[0])).AddHours(Int32.Parse(words[4])).AddMinutes(Int32.Parse(words[5]));
        }


        //-----------------
    }    
}
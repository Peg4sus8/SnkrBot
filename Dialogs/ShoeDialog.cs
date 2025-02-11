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
                    var attachments = shoes.Select(shoe => CreateShoeCard(shoe)).ToList();
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

        private static HeroCard CreateShoeCard(Shoe shoe)
        {
            var heroCard = new HeroCard
            {
                Title = shoe.Name,
                Subtitle = shoe.Release,
                Text = "Price: " + shoe.Price,
                Images = new List<CardImage> { new CardImage(shoe.Img) },
                Buttons = new List<CardAction> { new CardAction(ActionTypes.OpenUrl, "Get Started", value: "https://docs.microsoft.com/bot-framework") },
            };

            return heroCard;
        }
    }
}
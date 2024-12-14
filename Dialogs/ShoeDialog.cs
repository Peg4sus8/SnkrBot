using Microsoft.Bot.Builder.Dialogs;
using SnkrBot.Models;
using SnkrBot.Services;
using Newtonsoft.Json.Linq;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Builder;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using AdaptiveCards.Templating;
using System.IO;

namespace SnkrBot.Dialogs
{
    public class ShoeDialog : ComponentDialog
    {
        public ShoeDialog() : base(nameof(ShoeDialog))
        {
            var waterfallSteps = new WaterfallStep[]
            {
                ShowShoesAsync
            };

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> ShowShoesAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Scraping: ottieni i link e i dati delle scarpe
            var links = ScrapingUtility.GetLinks("https://heat-mvmnt.de/releases");
            var shoes = ScrapingUtility.GetCards(links);

            if (shoes.Any())
            {
                // Crea card interattive per ogni scarpa
                var attachments = shoes.Select(shoe => CreateShoeCard(shoe)).ToList();
                var reply = MessageFactory.Carousel(attachments);

                await stepContext.Context.SendActivityAsync(reply, cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync("Non sono state trovate scarpe disponibili al momento.");
            }

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

        private Attachment CreateShoeCard(Shoe shoe)
        {
            // Carica il template
            var templateJson = File.ReadAllText("ShoeCardTemplate.json");

            // Compila il template
            var template = new AdaptiveCardTemplate(templateJson);
            var data = new
            {
                name = shoe.Name,
                image = shoe.Img ?? "",
                releaseDate = shoe.Release ?? "N/A",
                price = ((shoe.Price.Equals("-", System.StringComparison.Ordinal)) ? "N/A": shoe.Price)
            };
            var cardJson = template.Expand(data);

            // Crea l'Attachment
            return new Attachment
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JObject.Parse(cardJson)
            };
        }
    }
}
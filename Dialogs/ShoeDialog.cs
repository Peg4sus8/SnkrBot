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
        private readonly string[] _validBrands = new[] { "Nike", "Adidas", "Jordan", "New Balance" }; // Aggiungi altri brand validi

        public ShoeDialog() : base(nameof(ShoeDialog))
        {
            var waterfallSteps = new WaterfallStep[]
            {
                ShowFilteredShoesAsync
            };

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> ShowFilteredShoesAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var filterDetails = (FilterDetails)stepContext.Options ?? new FilterDetails();

            // Scraping: ottieni i link e i dati delle scarpe
            var links = ScrapingUtility.GetLinks("https://heat-mvmnt.de/releases");
            var shoes = ScrapingUtility.GetCards(links);

            if (shoes.Any())
            {
                // Applica i filtri
                var filteredShoes = FilterShoes(shoes, filterDetails);

                if (!filteredShoes.Any())
                {
                    await stepContext.Context.SendActivityAsync(
                        "Non ho trovato scarpe che corrispondono ai criteri di ricerca specificati.",
                        cancellationToken: cancellationToken);
                }
                else
                {
                    // Crea card interattive per le scarpe filtrate
                    var attachments = filteredShoes.Select(shoe => CreateShoeCard(shoe)).ToList();
                    var reply = MessageFactory.Carousel(attachments);
                    await stepContext.Context.SendActivityAsync(reply, cancellationToken);
                }
            }
            else
            {
                await stepContext.Context.SendActivityAsync(
                    "Non sono state trovate scarpe disponibili al momento.",
                    cancellationToken: cancellationToken);
            }

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

        private IEnumerable<Shoe> FilterShoes(IEnumerable<Shoe> shoes, FilterDetails filters)
        {
            var query = shoes.AsQueryable();

            // Filtra per brand se specificato
            if (!string.IsNullOrEmpty(filters.Brand))
            {
                query = query.Where(s => s.Name.Contains(filters.Brand, System.StringComparison.OrdinalIgnoreCase));
            }

            // Filtra per prezzo minimo se specificato
            if (filters.MinPrice.HasValue)
            {
                query = query.Where(s => ParsePrice(s.Price) >= filters.MinPrice.Value);
            }

            // Filtra per prezzo massimo se specificato
            if (filters.MaxPrice.HasValue)
            {
                query = query.Where(s => ParsePrice(s.Price) <= filters.MaxPrice.Value);
            }

            return query.ToList();
        }

        private decimal ParsePrice(string price)
        {
            if (string.IsNullOrEmpty(price) || price == "-" || price == "N/A")
                return 0;

            // Rimuovi il simbolo della valuta e converti in decimal
            string numericPrice = new string(price.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
            if (decimal.TryParse(numericPrice, out decimal result))
                return result;

            return 0;
        }

        private Attachment CreateShoeCard(Shoe shoe)
        {
            var templateJson = File.ReadAllText("ShoeCardTemplate.json");
            var template = new AdaptiveCardTemplate(templateJson);
            var data = new
            {
                name = shoe.Name,
                image = shoe.Img ?? "",
                releaseDate = shoe.Release ?? "N/A",
                price = ((shoe.Price.Equals("-", System.StringComparison.Ordinal)) ? "N/A" : shoe.Price)
            };
            var cardJson = template.Expand(data);

            return new Attachment
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JObject.Parse(cardJson)
            };
        }
    }

    public class FilterDetails
    {
        public string Brand { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
    }
}
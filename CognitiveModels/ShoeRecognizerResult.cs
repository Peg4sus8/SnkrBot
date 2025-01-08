using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Bot.Builder;
using Newtonsoft.Json;

namespace SnkrBot.CognitiveModels
{
    public class ShoeRecognizerResult : IRecognizerConvert
    {
        public enum Intent
        {
            ShowAllShoes,
            FilterByBrand,
            FilterByPrice,
            None
        }

        public string Text { get; set; }
        public string AlteredText { get; set; }
        public Dictionary<Intent, IntentScore> Intents { get; set; }
        public ShoeEntities Entities { get; set; }

        public void Convert(dynamic result)
        {
            try
            {
                var jsonResult = JsonConvert.SerializeObject(result);
                var app = JsonConvert.DeserializeObject<ShoeRecognizerResult>(jsonResult);

                Text = app.Text;
                AlteredText = app.AlteredText;
                Intents = app.Intents ?? new Dictionary<Intent, IntentScore>();
                Entities = app.Entities ?? new ShoeEntities();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore durante la conversione: {ex.Message}");
                throw;
            }
        }

        public (Intent intent, double score) TopIntent()
        {
            if (Intents == null || !Intents.Any())
            {
                Console.WriteLine("Nessun intento trovato.");
                return (Intent.None, 0.0);
            }

            var maxIntent = Intent.None;
            var maxScore = 0.0;

            foreach (var entry in Intents)
            {
                if (entry.Value.Score > maxScore)
                {
                    maxIntent = entry.Key;
                    maxScore = entry.Value.Score;
                }
            }

            Console.WriteLine($"Top Intent: {maxIntent}, Score: {maxScore}");
            return (maxIntent, maxScore);
        }

        public static ShoeRecognizerResult FromRecognizerResult(RecognizerResult result)
        {
            try
            {
                return new ShoeRecognizerResult
                {
                    Text = result.Text,
                    AlteredText = result.AlteredText,
                    Intents = result.Intents?.ToDictionary(
                        intent => Enum.TryParse(typeof(ShoeRecognizerResult.Intent), intent.Key, out var parsedIntent)
                            ? (ShoeRecognizerResult.Intent)parsedIntent
                            : Intent.None,
                        intent => new IntentScore { Score = (double)(intent.Value.Score ?? 0.0) }
                    ) ?? new Dictionary<Intent, IntentScore>(),
                    Entities = JsonConvert.DeserializeObject<ShoeEntities>(JsonConvert.SerializeObject(result.Entities)) ?? new ShoeEntities()
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore durante la deserializzazione del RecognizerResult: {ex.Message}");
                throw;
            }
        }
    }

    public class IntentScore
    {
        public double Score { get; set; }
    }

    public class ShoeEntities
    {
        public string[] Brand { get; set; }

        [JsonProperty("number")]
        public double[] Price { get; set; }

        [JsonProperty("priceOperator")]
        public string[] PriceOperators { get; set; }

        public (double? minPrice, double? maxPrice) GetPriceRange()
        {
            if (Price == null || !Price.Any()) return (null, null);

            var priceOp = PriceOperators?.FirstOrDefault()?.ToLower() ?? "";

            switch (priceOp)
            {
                case "sotto":
                case "meno di":
                    return (null, Price[0]);
                case "sopra":
                case "più di":
                    return (Price[0], null);
                case "tra":
                    if (Price.Length >= 2)
                        return (Price[0], Price[1]);
                    break;
            }

            return (null, null);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Bot.Builder;
using Newtonsoft.Json;
 using Newtonsoft.Json.Linq;

namespace SnkrBot.CognitiveModels
{

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Schema;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

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

            public string Kind { get; set; } // Mappa "kind" dal JSON
            public ResultData Result { get; set; } // Mappa "result" dal JSON

            public void Convert(dynamic result)
            {
                try
                {
                    // Log del JSON originale
                    Console.WriteLine("JSON ricevuto:\n" + JsonConvert.SerializeObject(result, Formatting.Indented));

                    // Deserializza il JSON nella classe ShoeRecognizerResult
                    var deserialized = JsonConvert.DeserializeObject<ShoeRecognizerResult>(JsonConvert.SerializeObject(result));

                    Kind = deserialized.Kind;
                    Result = deserialized.Result;


                    // Log per il debug
                    Console.WriteLine($"Query: {Result.Query}");
                    Console.WriteLine($"Prediction: {JsonConvert.SerializeObject(Result?.Prediction, Formatting.Indented)}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Errore durante la conversione: {ex.Message}");
                    throw;
                }
            }

            public (Intent intent, double score) TopIntent()
            {
                if (Result?.Prediction?.Intents == null || !Result.Prediction.Intents.Any())
                {
                    Console.WriteLine("Nessun intento trovato.");
                    return (Intent.None, 0.0);
                }

                // Trova l'intento con il punteggio più alto
                var topIntent = Result.Prediction.Intents.OrderByDescending(i => i.ConfidenceScore).First();

                // Mappa la categoria dell'intento all'enum `Intent`
                if (Enum.TryParse(topIntent.Category, true, out Intent mappedIntent))
                {
                    return (mappedIntent, topIntent.ConfidenceScore);
                }

                return (Intent.None, 0.0);
            }
        }
        public class ResultData
        {
            public string Query { get; set; } // Mappa "query" dal JSON
            public PredictionResult Prediction { get; set; } // Mappa "prediction" dal JSON
        }

        public class PredictionResult
        {
            public string TopIntent { get; set; } // Mappa a "topIntent" nel JSON
            public List<PredictedIntent> Intents { get; set; } // Mappa a "intents" nel JSON
            public List<Entity> Entities { get; set; } // Mappa a "entities" nel JSON
        }
        public class Entity
        {
            public string Category { get; set; } // Mappa "category" dal JSON
            public string Text { get; set; } // Mappa "text" dal JSON
            public int Offset { get; set; } // Mappa "offset" dal JSON
            public int Length { get; set; } // Mappa "length" dal JSON
            public double ConfidenceScore { get; set; } // Mappa "confidenceScore" dal JSON
        }

        public class PredictedIntent
        {
            public string Category { get; set; } // Mappa a "category" nel JSON
            public double ConfidenceScore { get; set; } // Mappa a "confidenceScore" nel JSON
        }

    }

}

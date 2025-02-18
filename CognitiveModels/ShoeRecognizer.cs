using System;
using Microsoft.Bot.Builder;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text;
using Microsoft.BotBuilderSamples.Clu;
using Newtonsoft.Json;
using SnkrBot.CognitiveModels.SnkrBot.CognitiveModels;

namespace SnkrBot.CognitiveModels
{
    public class ShoeRecognizer
    {
        private readonly CluRecognizer _recognizer;
        private readonly string _endpointUrl;
        private readonly string _apiKey;
        private readonly string _projectName;
        private readonly string _deploymentName;
        private readonly string _subscriptionCode;

        public ShoeRecognizer(IConfiguration configuration)
        {
            _projectName = configuration["CLUProjectName"];
            _deploymentName = configuration["CLUDeploymentName"]; 
            _apiKey = configuration["CluAPIKey"];
            _endpointUrl = configuration["CluAPIEndpoint"];
            _subscriptionCode = configuration["CLUSubscriptionId"];


            var cluIsConfigured = !string.IsNullOrEmpty(configuration["CluProjectName"]) && !string.IsNullOrEmpty(configuration["CluDeploymentName"]) && !string.IsNullOrEmpty(configuration["CluAPIKey"]) && !string.IsNullOrEmpty(configuration["CluAPIEndpoint"]) && !string.IsNullOrEmpty(configuration["CLUSubscriptionId"]);
            if (cluIsConfigured)
            {
                var cluApplication = new CluApplication(_projectName, _deploymentName, _subscriptionCode, _endpointUrl);

                // Set the recognizer options depending on which endpoint version you want to use.
                var recognizerOptions = new CluOptions(cluApplication)
                {
                    Language = "it"
                };

                _recognizer = new CluRecognizer(recognizerOptions);
            }

            if (!Uri.IsWellFormedUriString(_endpointUrl, UriKind.Absolute))
            {
                throw new ArgumentException($"Endpoint URL non valido: {_endpointUrl}");
            }

           
        }

        public async Task<ShoeRecognizerResult> RecognizeAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var query = turnContext.Activity.Text;
            var requestUrl = $"{_endpointUrl}/language/:analyze-conversations?api-version=2022-10-01-preview";

            // Costruzione del corpo della richiesta
            var requestBody = new
            {
                kind = "Conversation",
                analysisInput = new
                {
                    conversationItem = new
                    {
                        id = "1",
                        text = query,
                        modality = "text",
                        language = "it",
                        participantId = "user"
                    }
                },
                parameters = new
                {
                    projectName = _projectName,
                    deploymentName = _deploymentName,
                    verbose = true,
                    stringIndexType = "TextElement_V8"
                }
            };

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _apiKey);

            try
            {
               /* Console.WriteLine("Inviando richiesta a CLU...");
                Console.WriteLine($"URL: {requestUrl}");
                Console.WriteLine($"Body: {System.Text.Json.JsonSerializer.Serialize(requestBody, new JsonSerializerOptions { WriteIndented = true })}");
               */
                var response = await httpClient.PostAsync(
                    requestUrl,
                    new StringContent(System.Text.Json.JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"));

                var responseBody = await response.Content.ReadAsStringAsync();

               /* Console.WriteLine("Risposta ricevuta da CLU:");
                Console.WriteLine(responseBody);*/

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Errore nella risposta CLU: {response.StatusCode} - {responseBody}");
                }

                dynamic result = JsonConvert.DeserializeObject<dynamic>(responseBody);
                var shoeRecognizerResult = new ShoeRecognizerResult();
                shoeRecognizerResult.Convert(result); // Chiama il metodo Convert per trasformare il risultato

                //Console.WriteLine($"Risultato deserializzato: {result}");

                return shoeRecognizerResult;
                //return System.Text.Json.JsonSerializer.Deserialize<ShoeRecognizerResult>(responseBody);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore durante la comunicazione con CLU: {ex.Message}");
                throw;
            }
        }

        public bool IsConfigured()
        {
            return !string.IsNullOrEmpty(_endpointUrl) &&
                   !string.IsNullOrEmpty(_apiKey) &&
                   !string.IsNullOrEmpty(_projectName) &&
                   !string.IsNullOrEmpty(_subscriptionCode) &&
                   !string.IsNullOrEmpty(_deploymentName);
        }

    }
}
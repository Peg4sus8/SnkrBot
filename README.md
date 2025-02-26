# SnkrBot

Un bot conversazionale specializzato nella ricerca e notifica di sneakers, creato con Bot Framework v4.

## Panoramica

SnkrBot è stato sviluppato utilizzando [Bot Framework](https://dev.botframework.com) e integra le seguenti funzionalità principali:

- Comprensione del linguaggio naturale tramite [Azure Language Service CLU](https://learn.microsoft.com/it-it/azure/cognitive-services/language-service/conversational-language-understanding/overview)
- Dialoghi multiturno per gestire conversazioni complesse
- Riconoscimento di intenti per ricerca scarpe per brand e prezzo
- Visualizzazione di risultati con card interattive
- Integrazione con Microsoft Teams e Microsoft Graph per aggiungere eventi al calendario

## Architettura

Il bot è strutturato secondo la seguente architettura:

```
SnkrBot/
├── Dialogs/
│   ├── MainDialog.cs     # Gestione del flusso principale di conversazione
│   └── ShoeDialog.cs     # Dialog specifico per visualizzare le scarpe
├── CognitiveModels/
│   ├── ShoeRecognizer.cs        # Integrazione con Azure Language CLU
│   └── ShoeRecognizerResult.cs  # Gestione risultati dell'analisi linguistica
├── Models/
│   ├── Shoe.cs           # Modello dati per le scarpe
│   └── FilterDetails.cs  # Struttura per i filtri di ricerca
└── Services/
    └── ShoeService.cs    # Servizio per l'accesso ai dati delle scarpe
```

## Intenti Supportati

Il bot è in grado di riconoscere i seguenti intenti:

1. **ShowAllShoes**: Mostra tutte le scarpe disponibili
2. **FilterByBrand**: Filtra le scarpe per marca specifica (es. "Mostrami le Nike")
3. **FilterByPrice**: Filtra le scarpe per prezzo massimo (es. "Scarpe sotto i 200€")
4. **ContinueOrExit**: Gestisce le risposte per continuare o terminare la conversazione

## Funzionalità Principali

### 1. Riconoscimento del Linguaggio

Il componente `ShoeRecognizer.cs` si connette ad Azure Language Service per analizzare le richieste dell'utente:

```csharp
public async Task<ShoeRecognizerResult> RecognizeAsync(ITurnContext turnContext, CancellationToken cancellationToken)
{
    var query = turnContext.Activity.Text;
    var requestUrl = $"{_endpointUrl}/language/:analyze-conversations?api-version=2022-10-01-preview";

    // Costruzione della richiesta per l'API CLU
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
    
    // Comunicazione con il servizio CLU...
}
```

### 2. Dialoghi Multi-turno

Il bot gestisce conversazioni complesse attraverso un sistema di dialoghi:

- `MainDialog`: Gestisce il flusso principale e indirizza agli altri dialoghi
- `ShoeDialog`: Gestisce la visualizzazione dei risultati di ricerca scarpe

### 3. Visualizzazione Scarpe con Card Interattive

`ShoeDialog.cs` crea card interattive per visualizzare le scarpe:

```csharp
private HeroCard CreateShoeCard(Shoe shoe, WaterfallStepContext stepContext)
{
    // Parsing della data di rilascio
    DateTime releaseDate;
    if (!DateTime.TryParse(shoe.Release, out releaseDate))
    {
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
```

### 4. Integrazione con Microsoft Teams

Il bot permette agli utenti di aggiungere promemoria al calendario di Teams per i lanci di nuove scarpe, utilizzando deep link nella card.

## Prerequisiti

- [.NET SDK](https://dotnet.microsoft.com/download) versione 6.0
- Una sottoscrizione Azure con Language Service configurato
- [Bot Framework Emulator](https://github.com/microsoft/botframework-emulator) per i test locali

## Configurazione

1. Crea una risorsa Language Service in Azure
2. Configura un progetto CLU con i seguenti intenti:
   - ShowAllShoes
   - FilterByBrand
   - FilterByPrice
   - ContinueOrExit
3. Configura le entità:
   - Brand (per riconoscere marche come Nike, Adidas, etc.)
   - Prezzi (per riconoscere valori numerici e valute)
   - PositiveReply (per riconoscere risposte affermative)
   - NegativeReply (per riconoscere risposte negative)
4. Addestra e pubblica il modello
5. Aggiorna `appsettings.json` con i parametri di connessione:

```json
{
  "CluAPIEndpoint": "Il tuo endpoint Language Service",
  "CluAPIKey": "La tua chiave Language Service",
  "CLUProjectName": "Il nome del tuo progetto CLU",
  "CLUDeploymentName": "Il nome del deployment CLU",
  "CLUSubscriptionId": "Il tuo subscription ID"
}
```

## Esecuzione locale

```bash
# Navigare nella cartella del progetto
cd SnkrBot

# Eseguire il bot
dotnet run
```

## Test con Bot Framework Emulator

1. Avvia Bot Framework Emulator
2. Seleziona "Open Bot"
3. Inserisci l'URL `http://localhost:3978/api/messages`
4. Inizia a chattare con il bot

## Esempi di interazione

- "Mostrami tutte le scarpe disponibili"
- "Cerco delle Nike"
- "Voglio vedere scarpe sotto i 200 euro"
- "Ho finito, grazie"

## Distribuzione su Azure

Per distribuire il bot su Azure, segui la documentazione ufficiale: [Distribuisci il tuo bot su Azure](https://aka.ms/azuredeployment)

## Risorse utili

- [Documentazione di Bot Framework](https://docs.botframework.com)
- [Fondamenti dei Bot](https://docs.microsoft.com/azure/bot-service/bot-builder-basics?view=azure-bot-service-4.0)
- [Dialoghi](https://docs.microsoft.com/it-it/azure/bot-service/bot-builder-concept-dialog?view=azure-bot-service-4.0)
- [Comprensione del linguaggio con CLU](https://learn.microsoft.com/it-it/azure/cognitive-services/language-service/conversational-language-understanding/overview)

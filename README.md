# SnkrBot

Un esempio di bot creato con Bot Framework v4.

Questo bot è stato creato utilizzando [Bot Framework](https://dev.botframework.com) e mostra come:

- Utilizzare [CLU](https://learn.microsoft.com/it-it/azure/cognitive-services/language-service/conversational-language-understanding/overview) per implementare capacità di intelligenza artificiale
- Implementare conversazioni multi-turno utilizzando i Dialogs
- Gestire le interruzioni dell'utente per richieste come `Aiuto` o `Annulla`
- Richiedere e validare informazioni dall'utente

## Prerequisiti

Questo esempio **richiede** prerequisiti per funzionare.

### Panoramica

Questo bot utilizza [CLU (Conversational Language Understanding)](https://learn.microsoft.com/it-it/azure/cognitive-services/language-service/conversational-language-understanding/overview), un servizio cognitivo basato sull'IA, per implementare la comprensione del linguaggio naturale.

### Installare .NET CLI

- [.NET SDK](https://dotnet.microsoft.com/download) versione 6.0

  ```bash
  # verificare la versione di dotnet
  dotnet --version
  ```

- Se non hai una sottoscrizione Azure, crea un [account gratuito](https://azure.microsoft.com/free/).
- Installa l'ultima versione dello strumento [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli?view=azure-cli-latest). Versione 2.0.54 o superiore.

### Creare un'applicazione CLU per abilitare la comprensione del linguaggio

Il modello CLU per questo esempio è configurato nel portale di Azure Language Service. Segui questi passaggi per configurare, addestrare e configurare l'applicazione CLU:

1. Crea una risorsa Language Service in Azure
2. Configura un progetto CLU e aggiungi intenti ed entità per riconoscere le richieste relative alle scarpe da ginnastica
3. Addestra e pubblica il modello
4. Ottieni le chiavi e gli endpoint necessari

Una volta creato il modello CLU, aggiorna `appsettings.json` con i tuoi valori:

```json
  "LanguageEndpoint": "Il tuo endpoint Language Service",
  "LanguageKey": "La tua chiave Language Service",
  "ProjectName": "Il nome del tuo progetto CLU",
  "DeploymentName": "Il nome del deployment CLU"
```

## Architettura

La struttura del bot è organizzata secondo il seguente schema:

![Architettura di SnkrBot](images/snkrbot-architecture.png)

L'architettura include:
- Un controller principale che gestisce le richieste HTTP
- Dialoghi per gestire le conversazioni con l'utente
- Integrazione con CLU per la comprensione del linguaggio naturale
- Connessione con database delle scarpe
- Integrazione con Microsoft Teams per la creazione di eventi in calendario

## Funzionalità

SnkrBot offre le seguenti funzionalità:

1. **Ricerca scarpe**: L'utente può cercare scarpe specificando marca, modello o altre caratteristiche
   ```csharp
   [LuisIntent("SearchShoes")]
   public async Task SearchShoes(IDialogContext context, LuisResult result)
   {
       var brand = GetEntityValue(result, "Brand");
       var model = GetEntityValue(result, "Model");
       
       // Ricerca scarpe nel database
       var shoes = await _shoeService.SearchShoes(brand, model);
       
       // Mostra risultati
       await ShowShoeResults(context, shoes);
   }
   ```

2. **Informazioni sui lanci**: Fornisce dettagli sui prossimi rilasci di scarpe
   ```csharp
   [LuisIntent("UpcomingReleases")]
   public async Task UpcomingReleases(IDialogContext context, LuisResult result)
   {
       var upcomingShoes = await _shoeService.GetUpcomingReleases();
       
       await context.PostAsync($"Ecco i prossimi {upcomingShoes.Count} lanci:");
       foreach (var shoe in upcomingShoes)
       {
           await SendShoeCard(context, shoe);
       }
   }
   ```

3. **Aggiunta di eventi al calendario**: Permette agli utenti di aggiungere promemoria nel calendario per i lanci di nuove scarpe
   ```csharp
   private HeroCard CreateShoeCard(Shoe shoe, WaterfallStepContext stepContext)
   {
       // Parsing della data di rilascio
       DateTime releaseDate;
       if (!DateTime.TryParse(shoe.Release, out releaseDate))
       {
           releaseDate = DateTime.Now.AddDays(7); // Data di fallback
       }
       
       // Calcola la data di fine evento (1 ora dopo l'inizio)
       var endTime = releaseDate.AddHours(1);
       
       // Formatta le date per il deep link
       var startTimeStr = releaseDate.ToString("yyyy-MM-ddTHH:mm:ss");
       var endTimeStr = endTime.ToString("yyyy-MM-ddTHH:mm:ss");
       
       // Creazione del deep link per l'evento del calendario
       var subjectEncoded = Uri.EscapeDataString(shoe.Name + " Release");
       var locationEncoded = Uri.EscapeDataString("Online Release");
       var bodyEncoded = Uri.EscapeDataString($"Release delle {shoe.Name} a {shoe.Price}");
       
       var calendarDeepLink = $"https://teams.microsoft.com/l/meeting/new?subject={subjectEncoded}&startTime={startTimeStr}&endTime={endTimeStr}&content={bodyEncoded}&location={locationEncoded}";
       
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

4. **Notifiche personalizzate**: Configura notifiche per particolari modelli o marche di interesse

## Provare questo esempio

- In un terminale, naviga fino a `SnkrBot`

    ```bash
    # cambia cartella al progetto
    cd SnkrBot
    ```

- Esegui il bot da un terminale o da Visual Studio, scegli l'opzione A o B.

  A) Da un terminale

  ```bash
  # esegui il bot
  dotnet run
  ```

  B) Oppure da Visual Studio

  - Avvia Visual Studio
  - File -> Apri -> Progetto/Soluzione
  - Naviga fino alla cartella `SnkrBot`
  - Seleziona il file `SnkrBot.csproj`
  - Premi `F5` per eseguire il progetto

## Testare il bot utilizzando Bot Framework Emulator

[Bot Framework Emulator](https://github.com/microsoft/botframework-emulator) è un'applicazione desktop che consente agli sviluppatori di bot di testare e debuggare i loro bot in localhost o in esecuzione remota attraverso un tunnel.

- Installa Bot Framework Emulator versione 4.5.0 o superiore da [qui](https://github.com/Microsoft/BotFramework-Emulator/releases)

### Connettiti al bot utilizzando Bot Framework Emulator

- Avvia Bot Framework Emulator
- File -> Open Bot
- Inserisci un URL del Bot di `http://localhost:3978/api/messages`

## Distribuire il bot su Azure

Per saperne di più sulla distribuzione di un bot su Azure, consulta [Distribuisci il tuo bot su Azure](https://aka.ms/azuredeployment) per un elenco completo delle istruzioni di distribuzione.

## Ulteriori letture

- [Documentazione di Bot Framework](https://docs.botframework.com)
- [Fondamenti dei Bot](https://docs.microsoft.com/azure/bot-service/bot-builder-basics?view=azure-bot-service-4.0)
- [Dialoghi](https://docs.microsoft.com/it-it/azure/bot-service/bot-builder-concept-dialog?view=azure-bot-service-4.0)
- [Raccolta di input utilizzando i prompt](https://docs.microsoft.com/it-it/azure/bot-service/bot-builder-prompts?view=azure-bot-service-4.0&tabs=csharp)
- [Elaborazione delle attività](https://docs.microsoft.com/it-it/azure/bot-service/bot-builder-concept-activity-processing?view=azure-bot-service-4.0)
- [Introduzione ad Azure Bot Service](https://docs.microsoft.com/azure/bot-service/bot-service-overview-introduction?view=azure-bot-service-4.0)
- [Documentazione di Azure Bot Service](https://docs.microsoft.com/azure/bot-service/?view=azure-bot-service-4.0)
- [Strumenti CLI .NET Core](https://docs.microsoft.com/it-it/dotnet/core/tools/?tabs=netcore2x)
- [Azure CLI](https://docs.microsoft.com/cli/azure/?view=azure-cli-latest)
- [Portale di Azure](https://portal.azure.com)
- [Comprensione del linguaggio con CLU](https://learn.microsoft.com/it-it/azure/cognitive-services/language-service/conversational-language-understanding/overview)
- [Canali e servizio Bot Connector](https://docs.microsoft.com/it-it/azure/bot-service/bot-concepts?view=azure-bot-service-4.0)

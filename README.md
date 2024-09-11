# Peer Review Client

## Descrizione del progetto
Il **Peer Review Client** è un'applicazione console progettata per gestire il processo di revisione tra pari per studenti delle scuole superiori. Gli studenti possono inviare compiti, ricevere feedback dai propri coetanei e visualizzare i voti ricevuti. Gli insegnanti possono creare lezioni, assegnare domande agli studenti e monitorare il processo di revisione.

## Funzionalità principali
### Per gli studenti:
- Visualizzare le lezioni e i relativi compiti.
- Inviare risposte ai compiti assegnati.
- Fornire feedback alle risposte dei compagni.
- Visualizzare i voti ricevuti.

### Per gli insegnanti:
- Creare nuove lezioni e compiti.
- Assegnare domande agli studenti.
- Monitorare le risposte e il feedback ricevuto dagli studenti.
- Aggiungere domande a una lezione già esistente.

## Struttura del codice

### Classi principali
- **Program.cs**: Contiene la logica di autenticazione e avvio dell'applicazione. Si occupa di gestire il ciclo principale del programma e l'interazione con l'utente.
- **Menu.cs**: Definisce il menu di interazione per studenti e insegnanti. Ogni menu contiene opzioni specifiche per ogni ruolo e implementa le azioni corrispondenti.
- **LocalCache.cs**: Gestisce la cache locale per ridurre il numero di richieste API e migliorare le prestazioni.
- **Localization.cs**: Gestisce la localizzazione dell'interfaccia, supportando le lingue italiana e inglese.
- **Helper.cs**: Contiene metodi di utilità, inclusi quelli per la formattazione dell'output in tabelle e per l'interazione con l'API.
  
## API
L'applicazione si interfaccia con un'API REST per recuperare e inviare dati. Alcuni endpoint principali:
- **Autenticazione** (`Login`): Effettua l'autenticazione dell'utente.
- **Lezioni** (`PeerReview/Lessons`): Consente di recuperare, creare e aggiornare le lezioni.
- **Feedback** (`PeerReview/Feedback`): Permette di inviare feedback sugli elaborati degli studenti.
- **Domande** (`PeerReview/Question`): Endpoint per gestire le domande assegnate agli studenti.

## Esempio di utilizzo
### Avvio del programma
Quando l'applicazione viene avviata, all'utente viene richiesto di autenticarsi inserendo email, password e ID del corso. Una volta autenticato, lo studente o l'insegnante può navigare attraverso il menu e scegliere diverse azioni come visualizzare le lezioni, inviare compiti o fornire feedback.

### Esempio di flusso per uno studente:
1. Autenticarsi.
2. Visualizzare le lezioni.
3. Inviare una risposta per un compito.
4. Dare feedback ai compagni.

### Esempio di flusso per un insegnante:
1. Autenticarsi.
2. Creare una nuova lezione.
3. Assegnare domande agli studenti.
4. Visualizzare i progressi degli studenti.

## Requisiti
- .NET Core SDK
- Un'istanza dell'API REST per l'integrazione

## Istruzioni per l'installazione
1. Clonare il repository del progetto.
2. Compilare il progetto utilizzando il comando `dotnet build`.
3. Eseguire il programma con `dotnet run`.

## Futuri sviluppi
- Aggiungere ulteriori funzionalità per gestire assenze e presenze degli studenti.
- Migliorare l'interfaccia utente e la gestione degli errori.


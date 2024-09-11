
# Guida allo Studente per l'utilizzo di Peer Review Client

*Requisiti*

### Software necessario
Prima di iniziare, assicurati di avere installati i seguenti software:

- **.NET Core SDK** (versione 5.0 o superiore): Puoi scaricarlo dal sito ufficiale [.NET Core SDK](https://dotnet.microsoft.com/download).

### Sistema operativo supportato
- Windows
- macOS
- Linux

## Installazione del progetto

### 1. Registrazione al servizio Baobab School 
Prima di scaricare e utilizzare il progetto, devi registrarti su **Baobab School**, la piattaforma utilizzata per il servizio di Peer Review. 
Segui questi passaggi per registrarti: 
1. Vai al sito [Baobab School](https://www.baobab.school/it/user/registrati). 
2. Compila il modulo di registrazione con le informazioni richieste (email, nome utente, password, ecc.). 
3. Conferma la registrazione cliccando sul link di verifica inviato via email.

**NOTA BENE**  DEVI USARE LA MAIL DELLA SCUOLA

### 2. Scaricare il progetto
Puoi ottenere il progetto scaricandolo da una delle seguenti fonti:
- Clona il repository GitHub (se hai git già installato sul tuo pc): 
 `https://github.com/FrancescoScuola/PeerReviewClient.git`
 
-   Oppure, ottieni una cartella compressa con i file del progetto.  Per scaricare il progetto, segui questi passaggi:

1. Vai alla pagina del repository GitHub del progetto:
2. Cerca il pulsante verde con scritto **Code** nella parte superiore della pagina.
3. Clicca su **Code** e seleziona l'opzione **Download ZIP**.
4. Verrà scaricata una cartella compressa (.zip) contenente tutti i file del progetto.

Dopo aver scaricato il file ZIP, decomprimilo utilizzando uno strumento come **WinRAR**, **7-Zip**, o lo strumento di decompressione integrato nel tuo sistema operativo (Windows, macOS o Linux).

Per estrarre il file ZIP:
1. Fai clic con il tasto destro sul file ZIP e seleziona **Estrai tutto...** (su Windows) oppure utilizza il comando di estrazione appropriato sul tuo sistema.
2. Specifica una destinazione dove salvare i file estratti.
3. Dopo l'estrazione, entra nella cartella contenente i file del progetto.

Ora sei pronto per procedere alla compilazione del progetto come descritto nei passaggi successivi.

### 3. Compilare il progetto

Una volta scaricato il progetto, apri un terminale o una console nella directory principale del progetto e digita il seguente comando per compilare il progetto:

    dotnet build
Questo comando analizzerà il codice, creerà i file binari e segnalerà eventuali errori di compilazione. Dopo aver completato la compilazione, nella directory del progetto verrà creata una cartella **bin**. Al suo interno troverai i file dell'applicazione compilata, inclusi gli eseguibili per il sistema operativo in uso.
Ora ... puoi usare iniziare ad usare l'applicazione!

### 4. Autenticazione

All'avvio del programma, ti verrà richiesto di inserire le tue credenziali di accesso:

1.  **Email**: Inserisci l'email con cui sei registrato al sistema di revisione.
2.  **Password**: Inserisci la tua password.
3.  **ID del corso**: Inserisci l'ID del corso a cui sei iscritto. Se è la prima volta che ti colleghi, inserisci il codice corso fornito dal docente.

### 5. Esempio di flusso di utilizzo

#### A. Visualizzazione delle lezioni disponibili

Una volta autenticato, puoi visualizzare le lezioni disponibili per il tuo corso. Dal menu principale, scegli l'opzione per visualizzare le lezioni. Apparirà una lista delle lezioni con i dettagli relativi a scadenze e domande a cui devi rispondere.

#### B. Invio di una risposta a una domanda

Per inviare una risposta a una domanda assegnata:

1.  Seleziona dal menu principale l'opzione per rispondere ai compiti.
2.  Visualizzerai un elenco di domande assegnate. Seleziona l'ID della domanda a cui vuoi rispondere.
3.  Scrivi la tua risposta e conferma l'invio.

#### C. Fornire feedback

Puoi anche fornire feedback alle risposte dei tuoi compagni. Dal menu principale:

1.  Seleziona l'opzione per dare feedback.
2.  Inserisci l'ID della lezione su cui vuoi dare feedback.
3.  Visualizza la domanda e la risposta del compagno, scrivi il tuo feedback e assegna un voto (da 4 a 8).

#### D. Visualizzare i voti ricevuti

Puoi controllare i voti ricevuti per i tuoi compiti scegliendo l'opzione corrispondente dal menu

#### Esempio di interazione con il programma

        Benvenuto in PeerReviewClient!
        
        Inserisci la tua mail: studente@example.com
        Inserisci la tua password: ********
        Inserisci l'ID del tuo corso o il codice fornito se è la prima volta che ti colleghi: 12345
        Inserisci il tuo ruolo (1 studente, 2 docente): 1
        
        [1] Visualizza Lezioni
        [2] Invia Compiti
        [3] Dai Feedback
        [4] Visualizza Voti
        [0] Esci
        > 1
       ---- Elenco Lezioni ---- 
       ID | Titolo | Scadenza

## Suggerimenti

-   Ricorda di salvare il tuo lavoro frequentemente prima di inviare risposte o feedback.
-   Controlla regolarmente le lezioni e le scadenze per evitare di perdere compiti importanti.
-   Se incontri problemi con l'autenticazione o altre funzionalità, contatta il tuo docente per assistenza.

###  Perché utilizzare una Console App invece di un'app con interfaccia grafica?

L'utilizzo di una **console app** ha diversi vantaggi, soprattutto in contesti educativi e di sviluppo rapido:
       
1. **L'uso di una console app evita l'uso del cellulare**, incoraggiando gli studenti a lavorare su PC o laptop, strumenti più adatti per lo studio.

2. **Gli studenti sono incoraggiati a sviluppare un'interfaccia grafica** durante l'anno, permettendo loro di espandere il progetto e acquisire competenze pratiche nella creazione di interfacce utente.

3.  **Semplicità e velocità di sviluppo**: Le console app sono più facili e veloci da sviluppare rispetto alle applicazioni con interfaccia grafica, poiché non richiedono la creazione di complessi elementi UI (bottoni, finestre, ecc.). Questo permette di concentrarsi più sul funzionamento del programma e meno sui dettagli dell'interfaccia utente.






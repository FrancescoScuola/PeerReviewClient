using System.Net.Http.Headers;
using System.Net;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using System.Text;
using Spectre.Console;
using System.Diagnostics;

namespace PeerReviewClient
{
    internal class Program
    {
        public static string filePath = "loginInfo.json";
        public static string sw_version = "0.5.0";
        public static string api_version = "0.5.0";
        // Sito per l'api
        public static int WEBSITE = 8;

        static async Task Main(string[] args)
        {
            try
            {

                var localization = new Localization("it");

                Console.WriteLine("Benvenuto in PeerReviewClient!");
                Console.WriteLine("");

                bool debugMode = IsDebugMode(args);

                var credentials = GetCredentials();

                //Console.Write("Press any key to start: ");
                //Console.ReadKey();

                // Creazione di un HttpClientHandler con CookieContainer
                var handler = new HttpClientHandler
                {
                    CookieContainer = new CookieContainer(),
                    UseCookies = true
                };

                using (var client = new HttpClient(handler))
                {
                    // Imposta l'URL base per il client
                    client.BaseAddress = new Uri(ApiHelper.GetApiBase(debugMode));
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    // Tentativo di login e controllo del ruolo
                    var loginResult = await AttemptLoginAsync(client, credentials);
                    if (loginResult.Result != ExecutionStatus.Done) {
                        AnsiConsole.MarkupLine($"[red]{loginResult.Message}[/]");
                        return;
                    }

                    Console.WriteLine();
                    var checkSwVesion = new VersionChecker(sw_version, loginResult.Value.swVersion.software_version);
                    switch (checkSwVesion.CompareVersions())
                    {
                        case VersionComparisonResult.Incompatible:
                            AnsiConsole.MarkupLine("[red]La versione del software è obsoleta. Aggiornare il software per poter continuare.[/]");
                            return;
                        case VersionComparisonResult.UpdateRecommended:
                            AnsiConsole.MarkupLine("[yellow]è presente una versione nuova! Aggiornare il software.[/]");
                            break;
                        case VersionComparisonResult.SameVersion:
                            AnsiConsole.MarkupLine("[darkgreen]La versione del software è aggiornata.[/]");
                            break;
                    }

                    var menuOptions = new MenuInitOptionsData()
                    {
                        client = client,
                        courseId = int.Parse(credentials.courseID),
                        localization = localization,
                        saveCredentials = credentials.isCredentialFileExist,
                        token = loginResult.Value.guidToken
                    };

                    IMenu menu;
                    if (credentials.role == PeerReviewRole.student)
                    {
                        menuOptions.role = PeerReviewRole.student;
                        menu = new StudentMenu(menuOptions);
                    }
                    else
                    {
                        menuOptions.role = PeerReviewRole.teacher;
                        menu = new TeacherMenu(menuOptions);
                    }

                    while (true)
                    {
                        var selectedOption = menu.GetMenuSelection();
                        if (selectedOption == 0) { break; }
                        await menu.ExecuteAction(selectedOption);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Error: " + ex.Message);
            }

            Console.WriteLine("Press any key to exit");
            var y = Console.ReadKey();


        }



        private static async Task<OperationResult<LoginResultData>> AttemptLoginAsync(HttpClient client, Credentials credentials)
        {
            var isLoginDone = false;
            var isRoleFound = false;

            var loginResultData = new LoginResultData
            {
                guidToken = new Guid(),
                swVersion = null
            };

            for (var countAttempt = 0; countAttempt < 3; countAttempt++)
            {
                Console.WriteLine($"Tentativo di autenticazione {countAttempt + 1} di 3");
                Console.WriteLine();

                using var cts = new CancellationTokenSource();
                if (countAttempt == 0)
                {
                    LoadAnimations(cts);
                }

                // Atutentificazione mail password
                if (isLoginDone == false)
                {
                    HttpResponseMessage loginResponse = await GetAuth(credentials, client);

                    // Stoppo l'animazione
                    cts.Cancel();

                    Console.WriteLine();
                    if (loginResponse.IsSuccessStatusCode == false)
                    {
                        AnsiConsole.MarkupLine($"[red]Errore nell'autenticazione: {loginResponse.StatusCode}[/]");
                        continue;
                    }
                    AnsiConsole.MarkupLine($"[green]1. Autenticazione avvenuta con successo![/]");


                    // Rispondo solo con il "token" di autentificazione
                    loginResultData.guidToken = await GetAuthToken(loginResponse);

                    // Utente non ancora iscritto al corso
                    // Non sa l'id del corso ma solo la password
                    if (int.TryParse(credentials.courseID, out var parseResult) == false)
                    {
                        if (EnrollStudent(credentials, client) == false)
                        {
                            continue;
                        }
                    }

                    isLoginDone = true;
                }

                Console.WriteLine("");
                Console.WriteLine($"Controllo del ruolo.");

                // Una volta ottenuto il token per l'autenticazione, controllo il ruolo
                if (int.TryParse(credentials.courseID, out _) == true)
                {
                    var testCheckRole = CheckRole(credentials, client);
                    if (testCheckRole.Result.Result == ExecutionStatus.Done)
                    {
                        credentials.isCredentialFileExist = testCheckRole.Result.Value.isCredentialsFound;
                        loginResultData.swVersion = testCheckRole.Result.Value.peerReviewRoleResponse;
                        isRoleFound = true;
                        break;
                    }
                }
            }

            if (loginResultData.swVersion == null)
            {
                return OperationResult<LoginResultData>.Fail("Errore nell'autenticazione");
            }


            if (isRoleFound == false)
            {
                return OperationResult<LoginResultData>.Fail("Errore nell'autenticazione");
            }

            return OperationResult<LoginResultData>.Ok(loginResultData);


        }

        private static void LoadAnimations(CancellationTokenSource cts)
        {
            // Avvia il finto task di caricamento
            var loadingTask = Task.Run(async () =>
            {
                try
                {
                    for (int i = 0; i < 10; i++)
                    {
                        if (cts.Token.IsCancellationRequested) break;
                        if (i == 0)
                        {
                            Console.Write("Caricamento in corso ...");
                        }
                        else
                        {
                            Console.Write(".");
                        }
                        await Task.Delay(1000, cts.Token); // Attende 1 secondo
                    }
                }
                catch (TaskCanceledException)
                {
                    // Ignora l'eccezione quando il task è cancellato
                }

            }, cts.Token);
        }

        private static bool IsDebugMode(string[] args)
        {
            var debugMode = false;

            if (args.Length > 0 && args[0] == "debug") { debugMode = true; }
            if (Debugger.IsAttached == true) { debugMode = true; }
            if (debugMode == true)
            {
                Console.WriteLine("Debug mode attivo");
            }

            return debugMode;
        }

        public static Credentials GetCredentials()
        {

            var loginManager = new LoginManager();
            var credentials = LoginManager.GetCredentialFromFile(filePath);
            var isCredentialsFound = credentials != null;

            if (credentials == null)
            {
                // File non trovato. Chiedo tramite console
                credentials = GetCredentialsFromConsole();
            }
            else
            {
                credentials.isCredentialFileExist = true;
            }

            return credentials;

        }

        public static Credentials GetCredentialsFromConsole()
        {
            string mail;
            string password;
            string? courseID;
            int role;

            while (true)
            {
                Console.Write("Inserisci la tua mail: ");
                mail = Console.ReadLine();

                Console.Write("Inserisci la tua password: ");
                password = ReadPassword();

                Console.Write("Inserisci l'ID del tuo corso o il codice fornito se è la prima volta che ti colleghi: ");
                courseID = Console.ReadLine();

                Console.Write($"Inserisci il tuo ruolo ({(int)PeerReviewRole.student} studente, {(int)PeerReviewRole.teacher} docente): ");
                var tRole = Console.ReadLine();
                var isValidRole = int.TryParse(tRole, out role);

                // Check if email is valid
                var isValiMail = new EmailAddressAttribute().IsValid(mail);

                if (string.IsNullOrEmpty(mail) || string.IsNullOrEmpty(password) || isValiMail == false || string.IsNullOrEmpty(courseID) || isValidRole == false)
                {
                    Console.WriteLine("Mail, ruolo o password non validi, riprova.");
                }
                else
                {
                    break;
                }
            }

            return new Credentials
            {
                email = mail,
                password = password,
                courseID = courseID,
                role = (PeerReviewRole)role
            };

        }
        public static string ReadPassword()
        {
            StringBuilder password = new StringBuilder();
            ConsoleKeyInfo key;

            do
            {
                key = Console.ReadKey(intercept: true); // intercept: true per non mostrare il carattere

                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    password.Append(key.KeyChar);
                    Console.Write("*"); // Mostra un asterisco per ogni carattere digitato
                }
                else if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                {
                    password.Remove(password.Length - 1, 1);
                    Console.Write("\b \b"); // Cancella l'ultimo carattere e il relativo asterisco
                }
            }
            while (key.Key != ConsoleKey.Enter); // Continua fino a quando non si preme Invio

            Console.WriteLine(); // Sposta il cursore alla riga successiva
            return password.ToString();
        }

        static async Task<Guid> GetAuthToken(HttpResponseMessage loginResponse)
        {
            var response = await loginResponse.Content.ReadAsStringAsync();
            var token = JsonConvert.DeserializeObject<dynamic>(response).token;
            var cleanToken = token.ToString().Replace("{", "").Replace("}", "");
            var guidToken = new Guid(cleanToken);
            return guidToken;
        }

        private static bool EnrollStudent(Credentials? credentials, HttpClient client)
        {
            // L'utente ha inserito la chiave di accesso. Devo inscriverlo al corso
            Console.WriteLine("Inserisci cognome e nome (COME NEL REGISTRO): ");
            var name = Console.ReadLine();
            while (true)
            {
                if (string.IsNullOrEmpty(name) == false)
                {
                    break;
                }
                else
                {
                    Console.WriteLine("Nome non valido");
                }
            }

            int registerNumber = -1;
            while (true)
            {
                Console.WriteLine("Inserisci numero del registro (COME NEL REGISTRO): ");
                var tempregisterNumber = Console.ReadLine();
                if (int.TryParse(tempregisterNumber, out registerNumber))
                {
                    break;
                }
                else
                {
                    Console.WriteLine("Numero del registro non valido");
                }
            }

            var enroll = new PeerReviewEnrollJsonData()
            {
                full_name = name,
                register_number = registerNumber,
                website = Program.WEBSITE,
                course_key = credentials.courseID,
            };

            var fetchEnrollResult = GetEnroll(enroll, client);
            if (fetchEnrollResult.Result.IsSuccessStatusCode)
            {
                Console.WriteLine("Iscrizione avvenuta con successo!");
                var courseID = fetchEnrollResult.Result.Content.ReadAsStringAsync().Result;
                Console.WriteLine("CourseID: " + courseID);
                credentials.courseID = courseID;
                return true;
            }
            else
            {
                Console.WriteLine("Errore nell'iscrizione al corso");
                return false;
            }
        }

        private static async Task<OperationResult<CheckRoleResult>> CheckRole(Credentials credentials, HttpClient client)
        {

            var result = new CheckRoleResult();

            bool isCredentialsFound = credentials.isCredentialFileExist;

            // Check role
            var role = new PeerReviewRoleJsonData()
            {
                course_class_id = int.Parse(credentials.courseID),
                role = (PeerReviewRole)credentials.role,
                website = Program.WEBSITE,
            };
            HttpResponseMessage roleResponse = await GetRole(role, client);
            Thread.Sleep(100);

            if (roleResponse.IsSuccessStatusCode)
            {
                AnsiConsole.MarkupLine($"[green]2. Controllo del ruolo avvenuto con successo![/]");

                var responseJson = await roleResponse.Content.ReadAsStringAsync();
                result.peerReviewRoleResponse = JsonConvert.DeserializeObject<PeerReviewRoleResponseJsonData>(responseJson);

                if (isCredentialsFound == false)
                {
                    var askForSave = DoesSaveCredentials();
                    if (askForSave)
                    {
                        result.isCredentialsFound = LoginManager.SaveLoginInfo(credentials, filePath);
                    }
                }
                return OperationResult<CheckRoleResult>.Ok(result);
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Errore nell'autorizzazione: {roleResponse.StatusCode}[/]");
                return OperationResult<CheckRoleResult>.Fail("Errore nell'autorizzazione");
            }

        }

        private static bool DoesSaveCredentials()
        {
            var save = true;
            while (true)
            {
                Console.WriteLine("");
                Console.WriteLine("Vuoi salvare le credenziali? (s/n): ");
                var responseSave = Console.ReadLine();
                if (responseSave == "s")
                {
                    save = true;
                    break;
                }
                else if (responseSave == "n")
                {
                    save = false;
                    break;
                }
            }
            return save;
        }

        private static async Task<HttpResponseMessage> GetAuth(Credentials credentials, HttpClient client)
        {
            LoginJsonData loginData = new()
            {
                email = credentials.email,
                password = credentials.password,
                website = Program.WEBSITE
            };

            var json = JsonConvert.SerializeObject(loginData);
            var data = new StringContent(json, Encoding.UTF8, "application/json");

            // Invia la richiesta di autenticazione
            var loginResponse = await client.PostAsync(ApiHelper.PostLogin(), data);
            return loginResponse;
        }

        private static async Task<HttpResponseMessage> GetRole(PeerReviewRoleJsonData roleData, HttpClient client)
        {
            var json = JsonConvert.SerializeObject(roleData);
            var data = new StringContent(json, Encoding.UTF8, "application/json");

            // Invia la richiesta di autenticazione
            var roleResponse = await client.PostAsync(ApiHelper.PostPeerReviewRole(), data);
            return roleResponse;
        }

        private static async Task<HttpResponseMessage> GetEnroll(PeerReviewEnrollJsonData enrollData, HttpClient client)
        {
            var json = JsonConvert.SerializeObject(enrollData);
            var data = new StringContent(json, Encoding.UTF8, "application/json");

            // Invia la richiesta di autenticazione
            var roleResponse = await client.PostAsync(ApiHelper.PostPeerReviewEnroll(), data);
            return roleResponse;
        }

    }
}

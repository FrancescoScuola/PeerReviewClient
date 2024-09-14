using Newtonsoft.Json;
using Spectre.Console;
using System.Text;

namespace PeerReviewClient
{
    public class LoginManager
    {
        private readonly HttpClient _client;
        private readonly Credentials _credentials;
        private readonly int _maxAttempts = 3;

        public LoginManager(HttpClient client, Credentials credentials)
        {
            _client = client;
            _credentials = credentials;
        }

        public async Task<OperationResult<LoginResultData>> AttemptLoginAsync()
        {
            var isLoginDone = false;
            var isRoleFound = false;

            var loginResultData = new LoginResultData
            {
                guidToken = new Guid(),
                swVersion = null,
            };

            for (var countAttempt = 0; countAttempt < _maxAttempts; countAttempt++)
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
                    HttpResponseMessage loginResponse = await GetAuth(_credentials, _client);

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
                    if (int.TryParse(_credentials.courseID, out var parseResult) == false)
                    {
                        if (EnrollStudent(_credentials, _client) == false)
                        {
                            continue;
                        }
                    }

                    isLoginDone = true;
                }

                Console.WriteLine("");
                Console.WriteLine($"Controllo del ruolo.");

                // Una volta ottenuto il token per l'autenticazione, controllo il ruolo
                if (int.TryParse(_credentials.courseID, out _) == true)
                {
                    var testCheckRole = CheckRole(_credentials, _client);
                    if (testCheckRole.Result.Result == ExecutionStatus.Done)
                    {
                        _credentials.isCredentialFileExist = testCheckRole.Result.Value.isCredentialsFound;
                        loginResultData.swVersion = testCheckRole.Result.Value.peerReviewRoleResponse;
                        loginResultData.CourseName = testCheckRole.Result.Value.peerReviewRoleResponse.class_name;
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
                    for (int i = 0; i < 20; i++)
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

        static async Task<Guid> GetAuthToken(HttpResponseMessage loginResponse)
        {
            var response = await loginResponse.Content.ReadAsStringAsync();
            var token = JsonConvert.DeserializeObject<dynamic>(response).token;
            var cleanToken = token.ToString().Replace("{", "").Replace("}", "");
            var guidToken = new Guid(cleanToken);
            return guidToken;
        }

        private static async Task<OperationResult<CheckRoleResult>> CheckRole(Credentials credentials, HttpClient client)
        {
            var result = new CheckRoleResult
            {
                isCredentialsFound = credentials.isCredentialFileExist
            };

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

                if (result.isCredentialsFound == false)
                {
                    var askForSave = DoesSaveCredentials();
                    if (askForSave)
                    {
                        result.isCredentialsFound = CredentialsManager.SaveLoginInfo(credentials);
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
            var roleResponse = await client.PostAsync(ApiHelper.PostRole(), data);
            return roleResponse;
        }

        private static async Task<HttpResponseMessage> GetEnroll(PeerReviewEnrollJsonData enrollData, HttpClient client)
        {
            var json = JsonConvert.SerializeObject(enrollData);
            var data = new StringContent(json, Encoding.UTF8, "application/json");

            // Invia la richiesta di autenticazione
            var roleResponse = await client.PostAsync(ApiHelper.PostEnroll(), data);
            return roleResponse;
        }

    }
}

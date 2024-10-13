using System.Net.Http.Headers;
using System.Net;
using Spectre.Console;
using System.Diagnostics;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace PeerReviewClient
{
    internal class Program
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static string filePath = "loginInfo.json";
        public static string sw_version = "0.6.5";
        public static string api_version = "0.6.2";
        // Sito per l'api
        public static int WEBSITE = 8;

        static async Task Main(string[] args)
        {
            try
            {
                InitLog();

                var localization = new Localization("it");

                Console.WriteLine("Benvenuto in PeerReviewClient!");
                Console.WriteLine("");
                Console.WriteLine("Version: " + sw_version + " - Api version: " + api_version);
                Console.WriteLine("");

                bool debugMode = IsDebugMode(args);               

                var credentialsManager = new CredentialsManager(filePath);
                var credentials = credentialsManager.GetCredentials();

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

                    // Tentativo di login
                    var loginManager = new LoginManager(client, credentials);
                    var loginResult = await loginManager.AttemptLoginAsync();

                    if (loginResult.Result != ExecutionStatus.Done)
                    {
                        AnsiConsole.MarkupLine($"[red]{loginResult.Message}[/]");
                        Console.WriteLine("Press any key to exit: ");
                        var y1 = Console.ReadKey();
                        return;
                    }

                    Console.WriteLine();
                    var courseMessage = $"[darkgoldenrod] Corso {credentials.courseID + " - " + loginResult.Value.CourseName} [/]";
                    var rule = new Rule(courseMessage);
                    AnsiConsole.Write(rule);
                    Console.WriteLine();

                    // Controllo versione software
                    var checkSwVersion = CheckVersion(loginResult.Value.swVersion.software_version);
                    AnsiConsole.MarkupLine(checkSwVersion.Message);
                    if (checkSwVersion.Result != ExecutionStatus.Done)
                    {
                        Console.WriteLine("Press any key to exit: ");
                        var y2 = Console.ReadKey();
                        return;
                    }

                    var menuOptions = new MenuInitOptionsData()
                    {
                        client = client,
                        courseId = int.Parse(credentials.courseID),
                        localization = localization,
                        saveCredentials = credentials.isCredentialFileExist,
                        token = loginResult.Value.guidToken,
                        role = credentials.role
                    };

                    Console.WriteLine("");
                    AnsiConsole.Markup($"- 0.6.4 Premi esc per tornare al menu principale");
                    Console.WriteLine("");
                    AnsiConsole.Markup($"- 0.6.5 [bold blue]NEW![/] Visualizza i voti tramite dashboard");
                    Console.WriteLine("");
                    Console.WriteLine("");

                    IMenu menu = CreateMenu(menuOptions);
                    await menu.InitMenu();

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
                logger.Error(ex, "Si è verificato un errore nel main.");
                Console.WriteLine();
                Console.WriteLine("Error: " + ex.Message);
            }

            Console.WriteLine("Press any key to exit: ");
            var y = Console.ReadKey();

        }

        private static void InitLog() {

            // Configurazione NLog via codice
            var config = new LoggingConfiguration();

            // Configura il target per scrivere i log su un file
            var fileTarget = new FileTarget("logfile")
            {
                FileName = "logs/logfile.log",
                Layout = "${longdate} | ${level:uppercase=true} | ${message} ${exception:format=tostring}"
            };

            // Aggiungi il target alla configurazione
            config.AddTarget(fileTarget);

            // Regola che manda tutti i log con livello >= Info al file
            var rule = new LoggingRule("*", LogLevel.Info, fileTarget);
            config.LoggingRules.Add(rule);

            // Applica la configurazione
            LogManager.Configuration = config;

            // Inizio dell'applicazione
            logger.Info("Applicazione avviata .....");

        }

        private static bool IsDebugMode(string[] args)
        {
            var debugMode = false;

            if (args.Length > 0 && args[0] == "debug") { debugMode = true; }
            if (Debugger.IsAttached == true) { debugMode = true; }
            if (debugMode == true)
            {
                AnsiConsole.MarkupLine("[darkgreen]Debug mode attivo[/]");
                Console.WriteLine("");
            }

            return debugMode;
        }

        private static OperationResult<string> CheckVersion(string remoteSwVersion)
        {
            Console.WriteLine();
            var checkSwVesion = new VersionChecker(sw_version, remoteSwVersion);
            switch (checkSwVesion.CompareVersions())
            {
                case VersionComparisonResult.Incompatible:
                default:
                    return OperationResult<string>.Fail("[red]La versione del software è obsoleta. Aggiornare il software per poter continuare.[/]");
                case VersionComparisonResult.UpdateRecommended:
                    return new OperationResult<string>(ExecutionStatus.Done, string.Empty, "[yellow]è presente una versione nuova! Aggiornare il software.[/]");
                case VersionComparisonResult.SameVersion:
                    return new OperationResult<string>(ExecutionStatus.Done, string.Empty, "[darkgreen]La versione del software è aggiornata.[/]");
            }

        }

        public static IMenu CreateMenu(MenuInitOptionsData menuOptions)
        {
            switch (menuOptions.role)
            {
                case PeerReviewRole.student:
                    return new StudentMenu(menuOptions);
                case PeerReviewRole.teacher:
                    return new TeacherMenu(menuOptions);
                default:
                    throw new ArgumentException("Ruolo non supportato");
            }
        }

    }
}

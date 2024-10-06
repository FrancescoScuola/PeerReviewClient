using Newtonsoft.Json;
using Spectre.Console;
using System.Text;
using System.Text.RegularExpressions;

namespace PeerReviewClient
{
    public class StudentMenu : BaseMenu
    {
        private StudentLocalCache _localCache { get; set; }

        public StudentMenu(MenuInitOptionsData options) : base((options))
        {
            this._localCache = new StudentLocalCache(courseId, token, role, options.client);            
        }

        public override async Task InitMenu()
        {
            await ShowStudentLessons();
            PrintToDoList();            
        }

        private void PrintToDoList()
        {
            var peerReviewClass = _localCache.GetStudentLessonSummaryDataAsync().Result.Value;
            var todo = false;
            Console.WriteLine(" ");
            AnsiConsole.MarkupLine("[darkgreen] -------- ToDo --------:[/]");
            Console.WriteLine(" ");

            foreach (var lesson in peerReviewClass)
            {
                var dateChecker = new DateChecker(DateTime.Now, lesson.first_deadline, lesson.second_deadline);
                var timeInterval = dateChecker.GetTimeInterval();
                switch (timeInterval)
                {
                    case TimeInterval.BeforeFirstDeadline:
                        if (lesson.count_questions_made < 2)
                        {
                            Console.WriteLine("- Rispondere alle domande lezione " + lesson.id + " - " + lesson.count_questions_made + "/2");
                            todo = true;
                        }
                        break;
                        
                    case TimeInterval.BetweenDeadlines:
                        if (lesson.count_feedback_made < 5)
                        {
                            Console.WriteLine("- Dare feedback lezione " + lesson.id + " - " + lesson.count_feedback_made + "/5");
                            todo = true;
                        }
                        break;
                    case TimeInterval.AfterSecondDeadline:                        
                        break;

                    default:
                        break;
                }
            }

            if (todo == false)
            {
                Console.WriteLine("Nessun compito da svolgere.");
            }

            Console.WriteLine(" ");
            AnsiConsole.MarkupLine("[darkgreen] ------------------------:[/]");
            Console.WriteLine(" ");

        }

        public override List<MenuOption> GetMenuOptions()
        {
            var menu = new List<MenuOption>();
            menu.Add(new MenuOption(this.localization.GetText(TranslateKey.SHOW_LESSONS), 1, ShowStudentLessons));
            menu.Add(new MenuOption(this.localization.GetText(TranslateKey.SUBMIT_ASSIGNMENT), 2, SubmitAssignment));
            menu.Add(new MenuOption(this.localization.GetText(TranslateKey.GIVE_FEEDBACK), 3, GiveFeedback));
            menu.Add(new MenuOption(this.localization.GetText(TranslateKey.VIEW_GRADES), 4, ViewGrades));
            menu.Add(new MenuOption(this.localization.GetText(TranslateKey.HOW_DO_GRADE), 10, HowToGrade));

            if (this.saveCredentials)
                menu.Add(new MenuOption(this.localization.GetText(TranslateKey.DELETE_CREDENTIALS), 11, DeleteCredentials));

            menu.Add(new MenuOption("Exit", 0, null));
            return menu;

        }

        private async Task HowToGrade()
        {
            DisplayTitle("Valutazione");

            var table = new Table();
            table.Border = TableBorder.Rounded;

            table.AddColumn("[bold]Criterio[/]");
            table.AddColumn("[bold]Descrizione[/]");
            table.AddColumn("[bold]Punteggio (4-8)[/]");

            table.AddRow("Chiarezza della Risposta", "La risposta è chiara, comprensibile e ben strutturata?", "4-8");
            table.AddRow("Completezza della Risposta", "La risposta affronta completamente tutte le parti della domanda?", "4-8");
            table.AddRow("Correttezza", "La risposta è corretta dal punto di vista concettuale e contenutistico?", "4-8");
            table.AddRow("Originalità", "La risposta dimostra pensiero critico o offre soluzioni creative?", "4-8");
            table.AddRow("Uso del Linguaggio", "La grammatica e il vocabolario utilizzati sono appropriati e corretti?", "4-8");
            table.AddRow("Rispettare le Linee Guida", "La risposta segue le istruzioni e i requisiti indicati dal compito?", "4-8");

            AnsiConsole.Write(table);

            AnsiConsole.MarkupLine("\n[bold]Scala di Valutazione:[/]");
            AnsiConsole.MarkupLine("[bold]4[/]: Non soddisfa affatto i requisiti.");
            AnsiConsole.MarkupLine("[bold]5[/]: Soddisfa parzialmente i requisiti, ma sono presenti errori significativi.");
            AnsiConsole.MarkupLine("[bold]6[/]: Soddisfa i requisiti in maniera sufficiente, con alcune aree di miglioramento.");
            AnsiConsole.MarkupLine("[bold]7[/]: Soddisfa bene i requisiti, con pochi errori o aree di miglioramento.");
            AnsiConsole.MarkupLine("[bold]8[/]: Eccelle in tutti gli aspetti, senza errori significativi.\n");

            AnsiConsole.MarkupLine("[bold]Feedback Costruttivo:[/]");
            AnsiConsole.MarkupLine("- Cosa ha fatto bene il compagno?");
            AnsiConsole.MarkupLine("- Come potrebbe migliorare la sua risposta?");
            AnsiConsole.MarkupLine("- Ci sono suggerimenti che potrebbero aiutarlo a migliorare?\n");

        }

        public async Task ShowStudentLessons()
        {
            DisplayTitle("Showing Lessons");

            var fetchData = await _localCache.GetStudentLessonSummaryDataAsync();

            if (fetchData.Result == ExecutionStatus.Done)
            {
                var peerReviewClass = fetchData.Value;
                if (peerReviewClass != null)
                {
                    DisplayMessage(" ");
                    var table = new TableHelper(this.studentsOptions, this.localization);
                    table.PrintStudentLessons(peerReviewClass);
                }
                else
                {
                    DisplayMessage("No lessons found.");
                }
            }
        }

        private async Task GiveFeedback()
        {
            DisplayTitle("Give Feedback");

            var lessonId = -1;
            while (true)
            {
                var temp = PromptForInlineInput("Lesson id: ");
                if (temp.Result != ExecutionStatus.Done)
                {
                    return;
                }

                if (int.TryParse(temp.Value, out lessonId) == false)
                {
                    DisplayMessage("Invalid input. Please try again.");
                    continue;
                }
                else
                {
                    break;
                }
            }

            var timeForFeedback = _localCache.GetStudentLessonSummaryDataAsync().Result.Value.FirstOrDefault(x => x.id == lessonId)?.first_deadline;
            if (timeForFeedback == null || timeForFeedback < DateTime.Now)
            {
                Console.WriteLine($"Non è ancora il momento di dare il feedback. Inizio ore: {timeForFeedback}");
                return;
            }


            var fetchResult = await _localCache.GetFeedbackAsync(lessonId);
            if (fetchResult.Result == ExecutionStatus.Done)
            {
                var feedback = fetchResult.Value;
                if (feedback != null)
                {
                    var feedbackData = GetFeedback(lessonId, feedback);
                    if (feedbackData.Result != ExecutionStatus.Done)
                    {
                        return;
                    }

                    if (SentFeedback(_localCache, feedbackData.Value))
                    {
                        _localCache.ResetCache();
                    }
                }
                else
                {
                    DisplayMessage("No feedback found.");
                }
            }
            else
            {
                DisplayMessage("Error getting feedback data.");
            }
        }

        private async Task SubmitAssignment()
        {
            DisplayTitle("Submitting Assignment");
            int lessonID = -1;
            var fetchData = await _localCache.GetToDoQuestionsAsync();
            if (fetchData.Result == ExecutionStatus.Done)
            {
                var todoQuestions = fetchData.Value;
                if (todoQuestions != null)
                {
                    if (todoQuestions.Count > 0)
                    {
                        DisplayMessage(" ");
                        DisplayMessage("Quale domanda vuoi ripondere?: ");

                        var table = new TableHelper(this.studentsOptions, this.localization);
                        table.PrintQuestions(todoQuestions);

                        // Get questionID
                        var questionIdResult = GetQuestionId(todoQuestions);
                        if (questionIdResult.Result != ExecutionStatus.Done)
                        {
                            return;
                        }

                        var questionId = questionIdResult.Value;
                        if (questionId != -1)
                        {

                            DisplayMessage(" ");
                            PeerReviewQuestionData questionTodo = null;
                            foreach (var lesson in todoQuestions)
                            {
                                if (lesson.lesson_questions.Any(q => q.id == questionId))
                                {
                                    questionTodo = lesson.lesson_questions.First(q => q.id == questionId);
                                    lessonID = lesson.id;
                                    break;
                                }
                            }

                            DisplayMessage($"Rispondi a '{questionTodo.question_text}'");
                            var answerResult = GetUserResponse();
                            var isChatGpt = 0;

                            // Ask if user used GPT
                            var isChatGptInput = PromptForInlineInput("Did you use GPT? (y/n): ");
                            if (isChatGptInput.Result != ExecutionStatus.Done)
                            {
                                return;
                            }
                            if (isChatGptInput.Value.ToLower() == "y")
                            {
                                isChatGpt = 1;
                            }

                            var checkForConfermation = PromptForInlineInput("Vuoi confermare la risposta? (s/n): ");
                            if (checkForConfermation.Result != ExecutionStatus.Done)
                            {
                                return;
                            }

                            if (checkForConfermation.Value.ToLower() == "s")
                            {
                                var submitAnswareResult = false;
                                if (answerResult.IsFilePresent)
                                {
                                    submitAnswareResult = SubmitPDFAnswareAsync(lessonID, questionId, answerResult.Response, answerResult.FilePath, isChatGpt).Result;
                                }
                                else
                                {
                                    submitAnswareResult = SubmitAnsware(questionId, answerResult.Response, isChatGpt);
                                }

                                if (submitAnswareResult)
                                {
                                    DisplayMessage("Answer submitted successfully.");
                                    _localCache.ResetCache();
                                }
                                else
                                {
                                    DisplayMessage("Error submitting answer.");
                                }
                            }
                            else
                            {
                                DisplayMessage("Answer not submitted.");
                            }
                        }
                    }
                    else
                    {
                        DisplayMessage("No assignment found.");
                    }
                }
                else
                {
                    DisplayMessage("No assignment found.");
                }
            }
            else
            {
                DisplayMessage("Error getting assignment data.");
            }
        }

        private bool SubmitAnsware(int questionId, string answer, int isChatGpt)
        {
            var peerReviewAnswer = new PeerReviewAnswererJsonData()
            {
                question_text = answer,
                course_class_id = this.courseId,
                question_id = questionId,
                role = PeerReviewRole.student,
                token = this.token,
                website = 8,
                is_chat_gpt = isChatGpt
            };

            var json = JsonConvert.SerializeObject(peerReviewAnswer);
            var data = new StringContent(json, Encoding.UTF8, "application/json");

            // Invia la risposta
            var response = Client.PostAsync("PeerReview/Answer", data);

            return response.Result.IsSuccessStatusCode;

        }

        private async Task<bool> SubmitPDFAnswareAsync(int lessonID, int questionId, string answer, string filePath, int isChatGpt)
        {
            string apiUrl = "PeerReview/Upload/Pdf";

            // Aggiungi eventuali informazioni addizionali che vuoi inviare
            var additionalData = new PeerReviewUpdatePdfJsonData()
            {
                website = 8,
                lesson_id = lessonID,
                question_id = questionId,
                question_text = answer,
                is_chat_gpt = isChatGpt,
                token = this.token,
                role = PeerReviewRole.student,
                file_path = filePath,
            };

            try
            {

                using var form = new MultipartFormDataContent();

                // Aggiungi il file PDF
                var fileContent = new ByteArrayContent(File.ReadAllBytes(filePath));
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
                form.Add(fileContent, "file", Path.GetFileName(filePath));

                // Aggiungi i dati addizionali come JSON
                var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(additionalData);
                form.Add(new StringContent(jsonData), "data");

                // Invia la richiesta
                HttpResponseMessage response = await Client.PostAsync(apiUrl, form);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("File caricato con successo!");
                    return true;
                }
                else
                {
                    Console.WriteLine($"Errore durante l'upload: {response.StatusCode}");
                    return false;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Errore: " + ex.Message);
            }

            return false;

        }
        private OperationResult<string> GetAnsware()
        {
            while (true)
            {
                var answer = PromptForInput("Answer: ");
                if (answer.Result != ExecutionStatus.Done)
                {
                    return OperationResult<string>.Fail("EscapeClick");
                }
                if (string.IsNullOrEmpty(answer.Value))
                {
                    DisplayMessage("Answer cannot be empty. Please try again.");
                }
                else
                {
                    return answer;
                }
            }
        }

        private OperationResult<int> GetQuestionId(List<PeerReviewLessonData> todoQuestions)
        {
            int questionId;
            while (true)
            {
                var tQuestionID = PromptForInlineInput("Question id: ");
                if (tQuestionID.Result != ExecutionStatus.Done)
                {
                    return OperationResult<int>.Fail("EscapeClick");
                }

                if (tQuestionID.Value == string.Empty)
                {
                    questionId = -1;
                    break;
                }

                if (int.TryParse(tQuestionID.Value, out questionId) == false)
                {
                    DisplayMessage("Invalid input. Please try again.");
                    continue;
                }
                // Check if question exists
                if (todoQuestions.Any(l => l.lesson_questions.Any(q => q.id == questionId)))
                {
                    break;
                }
                else
                {
                    DisplayMessage("Question not found. Please try again.");
                }
            }

            return OperationResult<int>.Ok(questionId);
        }
        private async Task ViewGrades()
        {
            DisplayTitle("Viewing Grades");

            var lessonId = -1;
            while (true)
            {
                var lessonIDResult = PromptForInlineInput("Lesson id: ");
                if (lessonIDResult.Result != ExecutionStatus.Done)
                {
                    return;
                }

                if (int.TryParse(lessonIDResult.Value, out lessonId) == false)
                {
                    DisplayMessage("Invalid input. Please try again.");
                    continue;
                }
                else
                {
                    var summary = _localCache.GetStudentLessonSummaryDataAsync().Result.Value;
                    var lesson = summary.FirstOrDefault(l => l.id == lessonId);
                    if (lesson != null)
                    {
                        break;
                    }
                    else
                    {
                        DisplayMessage("Lesson not found. Please try again.");
                    }
                    break;
                }
            }

            var fetchData = await _localCache.GetGradesAsync(lessonId);
            if (fetchData.Result == ExecutionStatus.Done)
            {
                var list = fetchData.Value;
                if (list.Count > 0)
                {
                    var table = new TableHelper(this.studentsOptions, this.localization);
                    table.PrintGrades(list);
                }
                else
                {
                    DisplayMessage("No grades found.");
                }
            }
            else
            {
                DisplayMessage("Error getting grades data.");
            }


        }
        public static UserResponse GetUserResponse()
        {
            Console.WriteLine("Scrivi la tua risposta (puoi inserire anche un file path):");

            // Regex per rilevare un percorso di file multi-piattaforma (Windows, Linux, macOS)
            string filePathPattern = @"[a-zA-Z]:\\(?:[^\\/:*?""<>|\r\n]+\\)*[^\\/:*?""<>|\r\n]+|(/[^/ ]*)+/?|~(/[^/ ]*)+/?";
            Regex regex = new Regex(filePathPattern);

            string response = string.Empty;
            string filePath = null;

            while (true)
            {
                var key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }
                else if (key.Key == ConsoleKey.Backspace && response.Length > 0)
                {
                    response = response.Substring(0, response.Length - 1);
                    Console.Write("\b \b");
                }
                else
                {
                    response += key.KeyChar;
                    Console.Write(key.KeyChar);
                }

                // Controlla se l'input contiene un percorso di file
                if (regex.IsMatch(response))
                {
                    filePath = regex.Match(response).Value;
                    if (File.Exists(filePath))
                    {
                        Markup.Escape("[green] File trovato![/]");
                        Console.WriteLine("");
                        break;
                    }
                }
            }

            return new UserResponse
            {
                Response = response,
                IsFilePresent = filePath != null,
                FilePath = filePath
            };
        }
    }

}

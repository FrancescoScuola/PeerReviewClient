using Newtonsoft.Json;
using Spectre.Console;
using System.Text;

namespace PeerReviewClient
{
    public interface IMenu
    {
        List<MenuOption> GetMenuOptions();
        int GetMenuSelection();
        string PromptForInput(string promptMessage);
        void DisplayMessage(string message);
        Task ExecuteAction(int optionId);
    }

    public abstract class BaseMenu : IMenu
    {
        protected int courseId { get; set; }
        protected Guid token { get; set; }
        protected HttpClient Client;
        protected bool saveCredentials { get; set; }
        protected PeerReviewRole role { get; set; }
        public LocalCache localCache { get; set; } = null;

        // TODO Prenderlo dall'api una volta fatto il login
        public AssignQuestionsToStudentsOptions studentsOptions { get; set; } = new AssignQuestionsToStudentsOptions();

        public Localization localization;

        public BaseMenu(MenuInitOptionsData options)
        {
            this.courseId = options.courseId;
            this.token = options.token;
            this.Client = options.client;
            this.role = options.role;
            this.saveCredentials = options.saveCredentials;
            this.localization = options.localization;
        }
        public abstract List<MenuOption> GetMenuOptions();

        private void DisplayMenu(List<MenuOption> options)
        {
            Console.WriteLine("");
            Console.WriteLine("Scegli l'opzione:");
            foreach (var option in options)
            {
                Console.WriteLine($" {option.Id}. {option.Description}");
            }
            Console.Write("> ");
        }

        public int GetMenuSelection()
        {
            var options = GetMenuOptions();
            DisplayMenu(options);
            while (true)
            {
                var isValidInt = int.TryParse(Console.ReadLine(), out int selection);
                if (isValidInt)
                {
                    var optionSelected = options.FirstOrDefault(o => o.Id == selection);
                    if (optionSelected != null)
                    {
                        return selection;
                    }
                }
                Console.WriteLine("Invalid selection. Please try again.");
            }
        }

        public virtual string PromptForInput(string promptMessage)
        {
            Console.WriteLine(promptMessage);
            return Console.ReadLine();
        }

        public virtual string PromptForInlineInput(string promptMessage)
        {
            Console.Write(promptMessage);
            return Console.ReadLine();
        }

        public virtual void DisplayMessage(string message)
        {
            Console.WriteLine(message);
        }

        public virtual void DisplayTitle(string title)
        {
            var messageToPrint = $"------ [gold3 bold] {title} [/] ------";
            AnsiConsole.MarkupLine(messageToPrint);
        }

        public async Task ExecuteAction(int optionId)
        {
            var allActions = GetMenuOptions();
            var actionToExecute = allActions.FirstOrDefault(a => a.Id == optionId);
            if (actionToExecute != null)
            {
                await actionToExecute.Action();
            }
            else
            {
                DisplayMessage("Invalid selection. Please try again.");
            }
        }

        public async Task<HttpResponseMessage> GetFromApi(string relativePath)
        {
            var getResponse = await this.Client.GetAsync(relativePath);
            return getResponse;
        }

        // Metodi comuni
        public async Task DeleteCredentials()
        {
            while (true)
            {
                var askConfirmation = PromptForInlineInput(localization.GetText(TranslateKey.DELETE_CREDENTIALS_CONFIRMATION));
                if (askConfirmation.ToLower() == localization.GetText(TranslateKey.CONFIRMATION_YES))
                {
                    CredentialsManager.DeleteLoginInfo();
                    DisplayMessage(localization.GetText(TranslateKey.DELETE_CREDENTIALS_DONE));
                    this.saveCredentials = false;
                    break;
                }
                else if (askConfirmation.ToLower() == localization.GetText(TranslateKey.CONFIRMATION_NO))
                {
                    break;
                }
                else
                {
                    DisplayMessage("Invalid input. Please try again.");
                }
            }
        }

    }

    public class StudentMenu : BaseMenu
    {
        public new StudentLocalCache localCache { get; set; }

        public StudentMenu(MenuInitOptionsData options) : base((options))
        {
            this.localCache = new StudentLocalCache(courseId, token, role, options.client);
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

            var fetchData = await localCache.GetStudentLessonSummaryDataAsync();

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
                if (int.TryParse(temp, out lessonId) == false)
                {
                    DisplayMessage("Invalid input. Please try again.");
                    continue;
                }
                else
                {
                    break;
                }
            }

            var fetchResult = await localCache.GetFeedbackAsync(lessonId);
            if (fetchResult.Result == ExecutionStatus.Done)
            {

                var feedback = fetchResult.Value;
                if (feedback != null)
                {
                    DisplayMessage(" ");
                    DisplayMessage("Question: " + feedback.question);
                    DisplayMessage("Answer: " + feedback.answer_text);
                    DisplayMessage(" ");
                    var feedbackText = PromptForInput("Your Feedback: ");

                    DisplayMessage(" ");
                    var grade = -1;
                    while (grade > 8 || grade < 4)
                    {
                        var temp = PromptForInlineInput("Grade (4-8): ");
                        if (int.TryParse(temp, out grade) == false)
                        {
                            DisplayMessage("Invalid input. Please try again.");
                        }
                    }

                    DisplayMessage(" ");
                    var missingElements = PromptForInput("Missing Elements: ");
                    DisplayMessage(" ");


                    DisplayMessage(" ");
                    // Chiedi se l'utente a cui sto facendo la peer review pensa che l'utente abbia usato GPT
                    var isChatGpt = 0;
                    var isChatGptInput = PromptForInlineInput("Did the student use GPT? (y/n): ");
                    if (isChatGptInput.ToLower() == "y")
                    {
                        isChatGpt = 1;
                    }

                    var feedbackData = new PeerReviewFeedbackDataJson()
                    {
                        lesson_id = lessonId,
                        id = feedback.id,
                        feedback_text = feedbackText,
                        grade = grade,
                        missing_elements = missingElements,
                        role = this.role,
                        token = this.token,
                        website = 8,
                        is_chat_gpt = isChatGpt
                    };

                    var confermation = PromptForInlineInput("Are you sure you want to submit the feedback? (y/n): ");
                    if (confermation.ToLower() == "y")
                    {
                        var postResult = localCache.Post(feedbackData, ApiHelper.PostPeerReviewFeedback());
                        if (postResult)
                        {
                            DisplayMessage("Feedback submitted successfully.");
                            localCache.ResetCache();
                        }
                        else
                        {
                            DisplayMessage("Error submitting feedback.");
                        }
                    }
                    else
                    {
                        DisplayMessage("Feedback not submitted.");
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

            var isPdf = false;

            var fetchData = await localCache.GetToDoQuestionsAsync();
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
                        var questionId = GetQuestionId(todoQuestions);
                        if (questionId != -1)
                        {

                            DisplayMessage(" ");
                            PeerReviewQuestionData questionTodo = null;
                            foreach (var lesson in todoQuestions)
                            {
                                if (lesson.lesson_questions.Any(q => q.id == questionId))
                                {
                                    questionTodo = lesson.lesson_questions.First(q => q.id == questionId);
                                    break;
                                }
                            }

                            DisplayMessage($"Rispondi a '{questionTodo.question_text}'");
                            var answer = GetAnsware();
                            if (answer.Trim().ToLower().EndsWith(".pdf"))
                            {
                                if (File.Exists(answer))
                                {
                                    isPdf = true;
                                }
                                else
                                {
                                    DisplayMessage("File not found. Please try again.");
                                    return;
                                }
                            }

                            var isChatGpt = 0;
                            // Ask if user used GPT
                            var isChatGptInput = PromptForInlineInput("Did you use GPT? (y/n): ");
                            if (isChatGptInput.ToLower() == "y")
                            {
                                isChatGpt = 1;
                            }

                            var checkForConfermation = PromptForInlineInput("Vuoi confermare la risposta? (s/n): ");
                            if (checkForConfermation.ToLower() == "s")
                            {
                                var submitAnswareResult = false;
                                if (isPdf)
                                {
                                    submitAnswareResult = SubmitPDFAnswareAsync(questionId, answer, isChatGpt).Result;
                                }
                                else
                                {
                                    submitAnswareResult = SubmitAnsware(questionId, answer, isChatGpt);
                                }

                                if (submitAnswareResult)
                                {
                                    DisplayMessage("Answer submitted successfully.");
                                    localCache.ResetCache();
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

        private async Task<bool> SubmitPDFAnswareAsync(int questionId, string answer, int isChatGpt)
        {
            string filePath = answer;
            string apiUrl = "PeerReview/Upload/Pdf";


            // Aggiungi eventuali informazioni addizionali che vuoi inviare
            var additionalData = new PeerReviewUpdatePdfJsonData()
            {
                website = 8,
                lesson_id = questionId,
                question_id = questionId
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


        private string GetAnsware()
        {
            while (true)
            {
                var answer = PromptForInput("Answer: ");
                if (string.IsNullOrEmpty(answer))
                {
                    DisplayMessage("Answer cannot be empty. Please try again.");
                }
                else
                {
                    return answer;
                }
            }
        }

        private int GetQuestionId(List<PeerReviewLessonData> todoQuestions)
        {
            int questionId;
            while (true)
            {
                var tQuestionID = PromptForInlineInput("Question id: ");
                if (tQuestionID == string.Empty)
                {
                    questionId = -1;
                    break;
                }

                if (int.TryParse(tQuestionID, out questionId) == false)
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

            return questionId;
        }
        private async Task ViewGrades()
        {
            DisplayTitle("Viewing Grades");

            var lessonId = -1;
            while (true)
            {
                var temp = PromptForInlineInput("Lesson id: ");
                if (int.TryParse(temp, out lessonId) == false)
                {
                    DisplayMessage("Invalid input. Please try again.");
                    continue;
                }
                else
                {
                    var summary = localCache.GetStudentLessonSummaryDataAsync().Result.Value;
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

            var fetchData = await localCache.GetGradesAsync(lessonId);
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

    }

    public class TeacherMenu : BaseMenu
    {
        public new TeacherLocalCache localCache { get; set; }

        public TeacherMenu(MenuInitOptionsData options) : base(options)
        {
            this.localCache = new TeacherLocalCache(courseId, token, role, options.client);
        }

        public override List<MenuOption> GetMenuOptions()
        {
            var menu = new List<MenuOption>();
            menu.Add(new MenuOption(this.localization.GetText(TranslateKey.SHOW_LESSONS), 1, ShowTeacherLessons));
            menu.Add(new MenuOption(this.localization.GetText(TranslateKey.ADD_LESSON), 2, AddLesson));
            menu.Add(new MenuOption(this.localization.GetText(TranslateKey.ADD_QUESTIONS_TO_LESSON), 3, AddQuestions));
            if (this.saveCredentials)
                menu.Add(new MenuOption(this.localization.GetText(TranslateKey.DELETE_CREDENTIALS), 11, DeleteCredentials));
            menu.Add(new MenuOption("Exit", 0, null));
            return menu;
        }

        private async Task AddLesson()
        {
            DisplayTitle("Adding Lesson");

            var title = PromptForInput("Title: ");
            var fistDeadline = PromptForInput("First Deadline in h (48 default): ");
            if (fistDeadline == string.Empty)
            {
                fistDeadline = "48";
            }

            var secondDeadline = PromptForInput("Second Deadline in h (120 default): ");
            if (secondDeadline == string.Empty)
            {
                secondDeadline = "120";
            }

            var firstDeadlineDate = DateTime.Now.AddHours(double.Parse(fistDeadline));
            var secondDeadlineDate = DateTime.Now.AddHours(double.Parse(secondDeadline));

            var lessonData = new PeerReviewLessonJsonData()
            {
                title = title,
                content_html = "",
                first_deadline = firstDeadlineDate,
                second_deadline = secondDeadlineDate,
                created_at = DateTime.Now,
                course_class_id = this.courseId,
                role = PeerReviewRole.teacher,
                token = this.token,
                website = 8,
            };

            var postResult = localCache.Post(lessonData, ApiHelper.PostPeerReviewLessons());

            if (postResult)
            {
                DisplayMessage("Lesson added successfully.");
                localCache.ResetCache();
            }
            else
            {
                DisplayMessage("Error adding lesson.");
            }

        }

        private async Task ShowTeacherLessons()
        {
            DisplayTitle("Showing Lessons");

            var fetchData = await localCache.GetTeacherLessonSummaryDataAsync();

            if (fetchData.Result == ExecutionStatus.Done)
            {
                var peerReviewClass = fetchData.Value;
                if (peerReviewClass != null)
                {
                    DisplayMessage(" ");
                    var table = new TableHelper(this.studentsOptions, this.localization);
                    table.PrintTeacherLessons(peerReviewClass);
                }
                else
                {
                    DisplayMessage("No lessons found.");
                }
            }
        }

        private async Task AddQuestions()
        {
            DisplayTitle("Adding Questions");

            var lessonId = -1;
            while (true)
            {
                var tLessonID = PromptForInlineInput("Lesson id: ");
                if (int.TryParse(tLessonID, out lessonId) == false)
                {
                    DisplayMessage("Invalid input. Please try again.");
                    continue;
                }

                // Check if lesson exists
                var fetchData = await localCache.GetPeerReviewClassDataAsync();
                if (fetchData.Result == ExecutionStatus.Done)
                {
                    if (fetchData.Value.lessons.Any(l => l.id == lessonId))
                    {
                        break;
                    }
                    else
                    {
                        DisplayMessage("Lesson not found. Please try again.");
                    }
                }
                else
                {
                    DisplayMessage("Error getting class data...");
                }
            }

            DisplayMessage(localization.GetText(TranslateKey.INSERT_STUDENT_ABSENCE));
            var fetchStudents = await localCache.GetPeerReviewStudentsAsync();
            var listStudetsPresents = new List<int>();
            if (fetchStudents.Result == ExecutionStatus.Done)
            {
                var mangageAbsentStudents = new AbsentStudents(this, fetchStudents.Value);
                var allStudentsResult = mangageAbsentStudents.GetAbsentStudent();
                if (allStudentsResult.Result == ExecutionStatus.Done)
                {
                    foreach (var student in allStudentsResult.Value.Where(t => t.isPresent).ToList())
                    {
                        listStudetsPresents.Add(student.id);
                    }
                }
            }
            else
            {
                DisplayMessage("Errore nel recupero degli studenti");
                return;
            }


            DisplayMessage("Aggiungi le domande. Invio per fermarsi");

            List<PeerReviewQuestionJsonData> questions = new List<PeerReviewQuestionJsonData>();
            while (true)
            {
                var question = PromptForInlineInput($"Domanda {questions.Count + 1}: ");
                if (string.IsNullOrEmpty(question))
                {
                    if (questions.Count == 0)
                    {
                        DisplayMessage("Inserire almeno una domanda.");
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    questions.Add(new PeerReviewQuestionJsonData() { question_text = question });
                }
            }

            PeerReviewQuestionsJsonData itemToAdd = new()
            {
                created_at = DateTime.Now,
                course_class_id = this.courseId,
                lesson_id = lessonId,
                role = PeerReviewRole.teacher,
                website = 8,
                token = this.token,
                questions = questions,
                students = listStudetsPresents
            };

            // Invia la richiesta di inserimento delle domande
            var postResult = localCache.Post(itemToAdd, ApiHelper.PostPeerReviewQuestion());

            if (postResult)
            {
                DisplayMessage("Questions added successfully.");
                localCache.ResetCache();
            }
            else
            {
                DisplayMessage("Error adding lesson.");
            }
        }

    }

    /// <summary>
    /// Class to manage the list of absent students
    /// </summary>
    public class AbsentStudents
    {
        private TeacherMenu Menu { get; set; }
        private IEnumerable<Student> Students { get; set; } = new List<Student>();

        public AbsentStudents(TeacherMenu menu, IEnumerable<PeerReviewUserData> students)
        {
            this.Students = ConvertUserInStudent(students);
            this.Menu = menu;
        }

        private static IEnumerable<Student> ConvertUserInStudent(IEnumerable<PeerReviewUserData> list)
        {
            var result = new List<Student>();
            var i = 1;
            foreach (var item in list)
            {
                result.Add(new Student()
                {
                    id = item.id,
                    isPresent = true,
                    name = item.school_name,
                    registerNumber = i
                });
                i++;
            }
            return result;
        }

        public OperationResult<IEnumerable<Student>> GetAbsentStudent()
        {
            Menu.DisplayMessage("Inserisci il numero o la sequenza degli studenti assenti (separato dallo spazio)");

            var table = new TableHelper(Menu.studentsOptions, Menu.localization);
            table.PrintRegister(this.Students);

            while (true)
            {
                var userInput = Menu.PromptForInlineInput("Student ID: ");
                if (string.IsNullOrEmpty(userInput))
                {
                    break;
                }

                var listId = new List<int>();
                var listIdToConvert = userInput.Split(' ');

                foreach (var item in listIdToConvert)
                {
                    if (int.TryParse(item, out int tempID))
                    {
                        listId.Add(tempID);
                        Menu.DisplayMessage($"Add {item}");
                    }
                    else
                    {
                        Menu.DisplayMessage($"Failed to convert {item}");
                    }
                }

                foreach (var studentId in listId)
                {
                    var selectedItem = this.Students.Where(t => t.registerNumber == studentId).FirstOrDefault();
                    if (selectedItem != null)
                    {
                        selectedItem.isPresent = !selectedItem.isPresent;
                    }
                    else
                    {
                        Menu.DisplayMessage("Student not found. Please try again.");
                    }
                }

                table.PrintRegister(this.Students);

            }

            return OperationResult<IEnumerable<Student>>.Ok(this.Students);
        }

    }
}


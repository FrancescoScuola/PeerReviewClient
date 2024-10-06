using Spectre.Console;
using System.Diagnostics;
using System.Text;

namespace PeerReviewClient
{
    public interface IMenu
    {
        List<MenuOption> GetMenuOptions();
        int GetMenuSelection();
        OperationResult<string> PromptForInput(string promptMessage);
        void DisplayMessage(string message);
        Task ExecuteAction(int optionId);
        Task InitMenu();
    }

    public abstract class BaseMenu : IMenu
    {
        protected int courseId { get; set; }
        protected Guid token { get; set; }
        protected HttpClient Client;
        protected bool saveCredentials { get; set; }
        protected PeerReviewRole role { get; set; }

        // TODO Prenderlo dall'api una volta fatto il login
        public AssignQuestionsToStudentsOptions studentsOptions { get; set; } = new AssignQuestionsToStudentsOptions();

        public Localization localization;

        protected Singleton _singleton = Singleton.Instance;

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
                PrintError("Invalid selection. Please try again.");
            }
        }

        public virtual OperationResult<string> PromptForInput(string promptMessage)
        {
            Console.WriteLine(promptMessage);
            return GetUserInput();
        }

        public virtual OperationResult<string> PromptForInlineInput(string promptMessage)
        {
            Console.Write(promptMessage);
            return GetUserInput();
        }

        public virtual OperationResult<int> PromptForIntInlineInput(string promptMessage)
        {
            Console.Write(promptMessage);
            while (true)
            {
                var t = GetUserInput();
                if(t.Result != ExecutionStatus.Done)
                {
                    return OperationResult<int>.Fail("EscapeClick");
                }
                var isValidInt = int.TryParse(t.Value, out int selection);
                if (isValidInt)
                {
                    return OperationResult<int>.Ok(selection);
                }
                PrintError("Invalid selection. Please try again.");
            }
        }

        private static OperationResult<string> GetUserInput()
        {
            var input = new StringBuilder();  // Usato per accumulare il testo inserito

            while (true)
            {
                var key = Console.ReadKey(intercept: true);  // Legge un tasto senza mostrarlo a schermo

                if (key.Key == ConsoleKey.Escape)
                {
                    Console.WriteLine(" ");
                    Console.WriteLine(" ");
                    AnsiConsole.MarkupLine($"[red]ABORT[/] - Esco al menu principale.");
                    Console.WriteLine(" ");
                    return OperationResult<string>.Fail("EscapeClick");
                }
                else if (key.Key == ConsoleKey.Enter)
                {
                    // Se viene premuto "Enter", si conclude l'input
                    Console.WriteLine();  // Va a capo
                    return OperationResult<string>.Ok(input.ToString());
                }
                else if (key.Key == ConsoleKey.Backspace && input.Length > 0)
                {
                    // Se viene premuto "Backspace", rimuove l'ultimo carattere
                    input.Remove(input.Length - 1, 1);
                    Console.Write("\b \b");  // Cancella il carattere dalla console
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    // Aggiungi il carattere alla stringa se non è un tasto di controllo
                    input.Append(key.KeyChar);
                    Console.Write(key.KeyChar);  // Mostra il carattere nella console
                }
            }
        }

        public virtual void DisplayMessage(string message)
        {
            Console.WriteLine(message);
        }

        public virtual void DisplayTitle(string title)
        {
            if (Singleton.Instance.IsTimeMillisecondsPassed())
            {
                Console.WriteLine(" ");
                var messageToPrint = $"------ [gold3 bold] {title} [/] ------";
                AnsiConsole.MarkupLine(messageToPrint);
                Console.WriteLine(" ");
            }
            else {
                //Debugger.Break();
            }           
        }

        public virtual void PrintError(string message)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {message}");
            Console.WriteLine(" ");
        }
        public virtual void PrintSuccess(string message)
        {
            AnsiConsole.MarkupLine($"[green]Success:[/] {message}");
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
                PrintError("Invalid selection. Please try again.");
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
                if (askConfirmation.Result != ExecutionStatus.Done)
                {
                    return;
                }

                if (askConfirmation.Value.ToLower() == localization.GetText(TranslateKey.CONFIRMATION_YES))
                {
                    CredentialsManager.DeleteLoginInfo();
                    DisplayMessage(localization.GetText(TranslateKey.DELETE_CREDENTIALS_DONE));
                    this.saveCredentials = false;
                    break;
                }
                else if (askConfirmation.Value.ToLower() == localization.GetText(TranslateKey.CONFIRMATION_NO))
                {
                    break;
                }
                else
                {
                    PrintError("Invalid input. Please try again.");
                }
            }
        }

        public OperationResult<PeerReviewFeedbackDataJson> GetFeedback(int lessonId, PeerReviewAnswerForFeedbackData answerData)
        {
            DisplayMessage(" ");
            DisplayMessage("Question: " + answerData.question);
            DisplayMessage("Answer: " + answerData.answer_text);
            DisplayMessage(" ");
            var feedbackTextResult = PromptForInput("Your Feedback: ");
            if(feedbackTextResult.Result != ExecutionStatus.Done)
            {
                return OperationResult<PeerReviewFeedbackDataJson>.Fail("EscapeClick");
            }

            var feedbackText = feedbackTextResult.Value;
            DisplayMessage(" ");
            var grade = -1;
            while (grade > 8 || grade < 4)
            {
                var gradeResult = PromptForInlineInput("Grade (4-8): ");
                if(gradeResult.Result != ExecutionStatus.Done)
                {
                    return OperationResult<PeerReviewFeedbackDataJson>.Fail("EscapeClick");
                }
                if (int.TryParse(gradeResult.Value, out grade) == false)
                {
                    PrintError("Invalid input. Please try again.");
                }
            }

            DisplayMessage(" ");
            var missingElements = PromptForInput("Missing Elements: ");
            if (missingElements.Result != ExecutionStatus.Done)
            {
                return OperationResult<PeerReviewFeedbackDataJson>.Fail("EscapeClick");
            }

            DisplayMessage(" ");            
            DisplayMessage(" ");

            // Chiedi se l'utente a cui sto facendo la peer review pensa che l'utente abbia usato GPT
            var isChatGpt = 0;
            var isChatGptInputResult = PromptForInlineInput("Did the student use GPT? (y/n): ");
            if (isChatGptInputResult.Result != ExecutionStatus.Done)
            {
                return OperationResult<PeerReviewFeedbackDataJson>.Fail("EscapeClick");
            }

            var isChatGptInput = isChatGptInputResult.Value;
            if (isChatGptInput.ToLower() == "y")
            {
                isChatGpt = 1;
            }

            var feedbackData = new PeerReviewFeedbackDataJson()
            {
                lesson_id = lessonId,
                id = answerData.id,
                feedback_text = feedbackText,
                grade = grade,
                missing_elements = missingElements.Value,
                role = this.role,
                token = this.token,
                website = 8,
                is_chat_gpt = isChatGpt
            };
            return OperationResult< PeerReviewFeedbackDataJson>.Ok(feedbackData);
        }

        public bool SentFeedback(ILocalCache localCache, PeerReviewFeedbackDataJson feedbackData)
        {
            var confermation = PromptForInlineInput("Are you sure you want to submit the feedback? (y/n): ");
            if (confermation.Result != ExecutionStatus.Done)
            {
                return false;
            }

            if (confermation.Value.ToLower() == "y")
            {
                var postResult = localCache.Post(feedbackData, ApiHelper.PostFeedback());
                if (postResult)
                {
                    PrintSuccess("Feedback submitted successfully.");
                    return true;
                }
                else
                {
                    PrintError("Error submitting feedback.");
                }
            }
            else
            {
                DisplayMessage("Feedback not submitted.");
            }


            return false;
        }

        public abstract Task InitMenu();
    }




}


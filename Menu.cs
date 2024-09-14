using Spectre.Console;

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

    
   

}


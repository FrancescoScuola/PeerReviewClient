namespace PeerReviewClient
{

    public enum TranslateKey
    {
        ACTIVE_LESSON_LABEL,
        ACTIVE_FEEDBACK_LABEL,
        SHOW_LESSONS,
        SUBMIT_ASSIGNMENT,
        VIEW_GRADES,
        GIVE_FEEDBACK,
        DELETE_CREDENTIALS,
        DELETE_CREDENTIALS_CONFIRMATION,
        CONFIRMATION_YES,
        CONFIRMATION_NO,
        DELETE_CREDENTIALS_DONE,
        ADD_QUESTIONS_TO_LESSON,
        INSERT_STUDENT_ABSENCE,
        NAME,
        IS_PRESENT,
        ABSENT,
        PRESENT,
        ADD_LESSON,
        HOW_DO_GRADE,
        MARK_QUESTION,
    }

    public class Localization
    {
        private string _language;

        private Dictionary<TranslateKey, string> _dictionary = new Dictionary<TranslateKey, string>();

        public Localization(string language)
        {
            this._language = language;
            switch (language)
            {
                case "it":
                default:
                    _dictionary = GetItalianDictionary();
                    break;
                case "en":
                    _dictionary = GetEnglishDictionary();
                    break;
            }
        }

        private static Dictionary<TranslateKey, string> GetEnglishDictionary()
        {
            var dictionary = new Dictionary<TranslateKey, string>();
            dictionary.Add(TranslateKey.ACTIVE_LESSON_LABEL, " (ACTIVE) ");
            dictionary.Add(TranslateKey.ACTIVE_FEEDBACK_LABEL, " (ACTIVE) ");
            dictionary.Add(TranslateKey.SHOW_LESSONS, "Show Lessons");
            dictionary.Add(TranslateKey.SUBMIT_ASSIGNMENT, "Submit Assignment");
            dictionary.Add(TranslateKey.VIEW_GRADES, "View Grades");
            dictionary.Add(TranslateKey.GIVE_FEEDBACK, "Give Feedback");
            dictionary.Add(TranslateKey.DELETE_CREDENTIALS, "Delete Credentials");
            dictionary.Add(TranslateKey.DELETE_CREDENTIALS_CONFIRMATION, "Are you sure you want to delete the credentials? (y/n)");
            dictionary.Add(TranslateKey.CONFIRMATION_YES, "y");
            dictionary.Add(TranslateKey.CONFIRMATION_NO, "n");
            dictionary.Add(TranslateKey.DELETE_CREDENTIALS_DONE, "Credentials deleted.");
            dictionary.Add(TranslateKey.ADD_QUESTIONS_TO_LESSON, "Add questions to lesson");
            dictionary.Add(TranslateKey.INSERT_STUDENT_ABSENCE, "Insert the student's absence register number");
            dictionary.Add(TranslateKey.NAME, "Name");
            dictionary.Add(TranslateKey.IS_PRESENT, "Is present");
            dictionary.Add(TranslateKey.ABSENT, "Absent");
            dictionary.Add(TranslateKey.PRESENT, "Present");
            dictionary.Add(TranslateKey.ADD_LESSON, "Add Lesson");
            dictionary.Add(TranslateKey.HOW_DO_GRADE, "How do grade?");
            dictionary.Add(TranslateKey.MARK_QUESTION, "Mark question");

            return dictionary;
        }

        private static Dictionary<TranslateKey, string> GetItalianDictionary()
        {
            var dictionary = new Dictionary<TranslateKey, string>();
            dictionary.Add(TranslateKey.ACTIVE_LESSON_LABEL, " (ATTIVO) ");
            dictionary.Add(TranslateKey.ACTIVE_FEEDBACK_LABEL, " (ATTIVO) ");
            dictionary.Add(TranslateKey.SHOW_LESSONS, "Mostra lezioni");
            dictionary.Add(TranslateKey.SUBMIT_ASSIGNMENT, "Rispondi alle domande");
            dictionary.Add(TranslateKey.VIEW_GRADES, "Visualizza voti");
            dictionary.Add(TranslateKey.GIVE_FEEDBACK, "Dai feedback");
            dictionary.Add(TranslateKey.DELETE_CREDENTIALS, "Cancella credenziali");
            dictionary.Add(TranslateKey.DELETE_CREDENTIALS_CONFIRMATION, "Sei sicuro di voler cancellare le credenziali? (s/n)");
            dictionary.Add(TranslateKey.CONFIRMATION_YES, "s");
            dictionary.Add(TranslateKey.CONFIRMATION_NO, "n");
            dictionary.Add(TranslateKey.DELETE_CREDENTIALS_DONE, "Credenziali cancellate.");
            dictionary.Add(TranslateKey.ADD_QUESTIONS_TO_LESSON, "Aggiungi domande alla lezione");
            dictionary.Add(TranslateKey.INSERT_STUDENT_ABSENCE, "Inserisci il numero del registro dello studente assente");
            dictionary.Add(TranslateKey.NAME, "Nome");
            dictionary.Add(TranslateKey.IS_PRESENT, "Presenza");
            dictionary.Add(TranslateKey.ABSENT, "Assente");
            dictionary.Add(TranslateKey.PRESENT, "Presente");
            dictionary.Add(TranslateKey.ADD_LESSON, "Aggiungi lezione");
            dictionary.Add(TranslateKey.HOW_DO_GRADE, "Come valutare?");
            dictionary.Add(TranslateKey.MARK_QUESTION, "Correggi domanda");


            return dictionary;
        }

        public string GetText(TranslateKey key)
        {
            if (_dictionary.ContainsKey(key))
            {
                return _dictionary[key];
            }
            return "{"+ key.ToString() + "}";
        }
    }
}

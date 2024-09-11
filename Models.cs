namespace PeerReviewClient
{
    public enum PeerReviewRole { student = 1, teacher = 2, admin = 3 }

    public enum ExecutionStatus
    {
        Error = 0,
        Done = 1,
        NotFound = 2,
        InvalidInput = 3,
        UnauthorizedAccess = 4,
        Forbidden = 5,
        Timeout = 6,
        Conflict = 7,
        TooManyRequests = 8,
        InternalServerError = 9,
        ServiceUnavailable = 10,
        CustomError = 11,
        Database = 12,
    }

    public enum TimeInterval
    {
        BeforeFirstDeadline,
        BetweenDeadlines,
        AfterSecondDeadline
    }

    public enum VersionComparisonResult
    {
        SameVersion,
        UpdateRecommended,
        Incompatible
    }

    public enum Color { green, red, yellow , lightslategrey , lightslateblue }
    
    public class Credentials
    {
        public string email { get; set; }
        public string password { get; set; }
        public string courseID { get; set; }
        public PeerReviewRole role { get; set; }
    }

    // Classe per inizializiare il menu
    public class MenuInitOptionsData
    {
        public int courseId { get; set; }
        public Guid token { get; set; }
        public PeerReviewRole role { get; set; }
        public HttpClient client { get; set; }
        public bool saveCredentials { get; set; }
        public Localization localization { get; set; }


        //public MenuInitOptionsData(int courseId, Guid token, PeerReviewRole role, HttpClient client, bool saveCredentials, Localization localization)
        //{
        //    this.courseId = courseId;
        //    this.token = token;
        //    this.role = role;
        //    this.client = client;
        //    this.saveCredentials = saveCredentials;
        //    this.localization = localization;
        //}
    }

    public class MenuOption
    {
        public MenuOption(string description, int id, Func<Task> action)
        {
            Description = description;
            Id = id;
            Action = action;
        }

        public string Description { get; set; }
        public int Id { get; set; }
        public Func<Task> Action { get; set; }
    }

    /// <summary>
    /// Is a generic class that can be used to return the result of an operation.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class OperationResult<T>
    {
        public ExecutionStatus Result { get; private set; } = ExecutionStatus.Error;
        public string Message { get; private set; } = string.Empty;
        public T Value { get; private set; }

        public OperationResult()
        {
        }

        public OperationResult(ExecutionStatus result, string message, T value = default)
        {
            Result = result;
            Message = message;
            Value = value;
        }

        public OperationResult(ExecutionStatus result, T value = default)
        {
            Result = result;
            Value = value;
        }

        public static OperationResult<T> Ok(T value = default)
        {
            return new OperationResult<T>(ExecutionStatus.Done, value);
        }

        public static OperationResult<T> Fail(string message)
        {
            return new OperationResult<T>(ExecutionStatus.Error, message);
        }
    }

    public class Student
    {
        public int id { get; set; }
        public string name { get; set; }
        public int registerNumber { get; set; }
        public bool isPresent { get; set; } = true;

    }

    // SOLO PER TABELLE
    public class StudentLessonsTableData
    {
        public int id { get; set; }
        public string title { get; set; }
        public string created_at { get; set; }
        public string first_deadline { get; set; }
        public string second_deadline { get; set; }
        public string count_questions_made { get; set; }
        public string count_feedback_made { get; set; }
    }

    public class TeacherLessonTableData
    {
        public string id { get; set; }
        public string title { get; set; }
        public string created_at { get; set; }
        public string first_deadline { get; set; }
        public string second_deadline { get; set; }
        public string count_questions { get; set; }
        public string count_questions_made { get; set; }
        public string answered_questions_range { get; set; }
        public string count_feedback_made_range { get; set; }
    }

    #region JSON Data   

    public class LoginJsonData
    {
        public string email { get; set; }
        public string password { get; set; }
        public int website { get; set; } = 8;
    }

    public class PeerReviewRoleJsonData
    {
        public PeerReviewRole role { get; set; }
        public int course_class_id { get; set; }
        public int website { get; set; }
    }

    public class PeerReviewLessonJsonData
    {
        public string title { get; set; }
        public DateTime created_at { get; set; }
        public DateTime first_deadline { get; set; }
        public DateTime second_deadline { get; set; }
        public string content_html { get; set; }
        public int course_class_id { get; set; }
        public int website { get; set; }
        public Guid token { get; set; }
        public PeerReviewRole role { get; set; }
    }

    public class PeerReviewQuestionsJsonData
    {
        public List<PeerReviewQuestionJsonData> questions { get; set; }
        public DateTime created_at { get; set; }
        public int course_class_id { get; set; }
        public int lesson_id { get; set; }
        public PeerReviewRole role { get; set; }
        public int website { get; set; }
        public Guid token { get; set; }
        public List<int> students { get; set; }
    }

    public class PeerReviewQuestionJsonData
    {
        public int id { get; set; }
        public string question_text { get; set; }
        public DateTime created_at { get; set; }
    }

    public class PeerReviewAnswererJsonData
    {
        public int question_id { get; set; }
        public string question_text { get; set; }
        public PeerReviewRole role { get; set; }
        public int course_class_id { get; set; }
        public int website { get; set; }
        public Guid token { get; set; }
        public int is_chat_gpt { get; set; }
    }

    public class PeerReviewEnrollJsonData
    {
        public string course_key { get; set; }
        public string full_name { get; set; }
        public int register_number { get; set; }
        public int website { get; set; }
    }

    public class PeerReviewRoleResponseJsonData
    {
        public string message { get; set; }
        public string api_version { get; set; }
        public string software_version { get; set; }
        public string class_name { get; set; }
        public string role { get; set; }
    }

    public class CheckRoleResult {
        public PeerReviewRoleResponseJsonData peerReviewRoleResponse { get; set; }
        public bool isCredentialsFound { get; set; }
    }

    #endregion

    #region Model Data API

    public class PeerReviewClassData
    {
        public int id { get; set; }
        public string name { get; set; }
        public string code { get; set; }
        public string school_code { get; set; }
        public int admin_id { get; set; }
        public ICollection<PeerReviewUserData> class_users { get; set; } = new List<PeerReviewUserData>();
        public ICollection<PeerReviewLessonData> lessons { get; set; } = new List<PeerReviewLessonData>();
    }

    public class PeerReviewUserData
    {
        public int id { get; set; }
        public string nickname { get; set; }
        public string school_name { get; set; }
        public int order { get; set; }
        public ICollection<PeerReviewAnswerData> answers { get; set; } = new List<PeerReviewAnswerData>();
        public ICollection<PeerReviewFeedbackData> feedbacks { get; set; } = new List<PeerReviewFeedbackData>();
    }

    public class PeerReviewLessonData
    {
        public int id { get; set; }
        public string title { get; set; }
        public DateTime created_at { get; set; }
        public DateTime? first_deadline { get; set; }
        public DateTime? second_deadline { get; set; }
        public string content_html { get; set; }
        public int class_id { get; set; }
        public ICollection<PeerReviewQuestionData> lesson_questions { get; set; }
        public string reservations_list_json { get; set; }
    }

    public class PeerReviewQuestionData
    {
        public int id { get; set; }
        public string question_text { get; set; }
        public DateTime? created_at { get; set; }
        public ICollection<PeerReviewAnswerData> answers { get; set; }
        public int class_id { get; set; }
    }

    public class PeerReviewAnswerData
    {
        public int id { get; set; }
        public int question_id { get; set; }
        public int user_id { get; set; }
        public string answer_text { get; set; }
        public DateTime? created_at { get; set; }
        public ICollection<PeerReviewFeedbackData> feedbacks { get; set; }
        public string question_text { get; set; }
    }

    public class PeerReviewFeedbackData
    {
        public int id { get; set; }
        public int user_id { get; set; }
        public string feedback_text { get; set; }
        public decimal grade { get; set; }
        public string missing_elements { get; set; }
        public int answer_id { get; set; }
        public DateTime? created_at { get; set; }
    }

    public class PeerReviewFeedbackDataJson
    {
        public int lesson_id { get; set; }
        public int id { get; set; }
        public string feedback_text { get; set; }
        public decimal grade { get; set; }
        public string missing_elements { get; set; }
        public PeerReviewRole role { get; set; }
        public int website { get; set; }
        public Guid token { get; set; }
        public int is_chat_gpt { get; set; }    
    }

    public class PeerReviewRoleData
    {
        public PeerReviewRole role { get; set; }
        public int class_id { get; set; }
        public int website { get; set; }
    }

    public class AssignQuestionsToStudentsOptions
    {
        public int QuestionsToAnswer { get; set; } = 2;
        public int FeedbacksToGive { get; set; } = 5;
    }

    public class QuestionsToStudentsData
    {
        public int StudentId { get; set; }
        public List<int> Questions { get; set; }
    }
    // ---------------------------------------------------------

    public class PeerReviewQuestionReservationData
    {
        public int id { get; set; }
        public int user_id { get; set; }
        public int questions_id { get; set; }

        // Lista degli utenti che hanno risposto a questa domanda
        public List<int> list_answer_made { get; set; } = new List<int>();

        // Lista degli utenti che risponderanno a questa domanda
        public List<int> list_assign_users { get; set; } = new List<int>();
    }

    public class PeerReviewOptionsReservationData
    {
        public int n_answer_for_questions { get; set; } = 2;
        public int n_max_feedback { get; set; } = 5;

        public PeerReviewOptionsReservationData(int nQuestions, int nStudents)
        {

        }

        public PeerReviewOptionsReservationData()
        {

        }
    }

    public class PeerReviewAnswerForFeedbackData
    {
        public int id { get; set; }
        public string question { get; set; }
        public string answer_text { get; set; }
    }


    // ---------------------------------------------------------

    public class PeerReviewSummaryLessonStudentData
    {
        public int id { get; set; }
        public string title { get; set; }
        public DateTime created_at { get; set; }
        public DateTime? first_deadline { get; set; }
        public DateTime? second_deadline { get; set; }
        public int count_questions_made { get; set; }
        public int count_feedback_made { get; set; }
    }

    public class PeerReviewSummaryLessonTeacherData
    {
        public int id { get; set; }
        public string title { get; set; }
        public DateTime? created_at { get; set; }
        public DateTime? first_deadline { get; set; }
        public DateTime? second_deadline { get; set; }
        public int count_questions { get; set; }
        public int count_questions_made { get; set; }
        public int total_answered_questions { get; set; }
        public int count_feedback_made { get; set; }
    }

    #endregion


}




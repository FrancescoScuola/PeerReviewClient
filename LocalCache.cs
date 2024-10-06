using Newtonsoft.Json;
using Spectre.Console;
using System.Text;

namespace PeerReviewClient
{
    public interface ILocalCache
    {
        Task<HttpResponseMessage> GetFromApi(string relativePath);
        bool Post(object item, string url);
    }

    public class LocalCache : ILocalCache
    {

        public static void PrintCacheMiss() { Console.WriteLine(); AnsiConsole.MarkupLine($"[lightslategrey]Cache Miss[/]"); }
        public static void PrintCacheHit() { Console.WriteLine(); AnsiConsole.MarkupLine($"[lightslateblue]Cache Hit[/]"); }

        protected int courseId { get; set; }
        protected Guid token { get; set; }
        protected HttpClient client;
        protected PeerReviewRole role { get; set; }

        public LocalCache(int courseId, Guid token, PeerReviewRole role, HttpClient client)
        {
            this.courseId = courseId;
            this.token = token;
            this.client = client;
            this.role = role;
        }

        public async Task<HttpResponseMessage> GetFromApi(string relativePath)
        {
            var getResponse = await this.client.GetAsync(relativePath);
            return getResponse;
        }

        public bool Post(object item, string url)
        {
            var json = JsonConvert.SerializeObject(item);
            var data = new StringContent(json, Encoding.UTF8, "application/json");
            var response = this.client.PostAsync(url, data);
            return response.Result.IsSuccessStatusCode;
        }

    }

    public class StudentLocalCache : LocalCache
    {
        private List<PeerReviewSummaryLessonStudentData> _summary = null;

        private List<PeerReviewLessonData> _todoQuestions = null;

        private bool _isSummaryValid { get => _summary != null; }
        private bool _isToDoQuestionsValid { get => _todoQuestions != null; }

        public void ResetCache()
        {
            this._summary = null;
            this._todoQuestions = null;
        }

        public StudentLocalCache(int courseId, Guid token, PeerReviewRole role, HttpClient client) : base(courseId, token, role, client)
        {

        }

        public async Task<OperationResult<List<PeerReviewSummaryLessonStudentData>>> GetStudentLessonSummaryDataAsync()
        {
            if (_isSummaryValid == false)
            {
                PrintCacheMiss();
                var ulr = ApiHelper.GetStudentLessonsSummary(this.token, this.courseId, this.role);
                var result = await GetFromApi(ulr);
                if (result.IsSuccessStatusCode)
                {
                    var response = await result.Content.ReadAsStringAsync();
                    // convert response to PeerReviewClassData
                    var summary = JsonConvert.DeserializeObject<List<PeerReviewSummaryLessonStudentData>>(response);
                    if (summary != null)
                    {
                        this._summary = summary;
                        return OperationResult<List<PeerReviewSummaryLessonStudentData>>.Ok(summary);
                    }
                }
            }
            else
            {
                PrintCacheHit();
                return OperationResult<List<PeerReviewSummaryLessonStudentData>>.Ok(this._summary);
            }

            return OperationResult<List<PeerReviewSummaryLessonStudentData>>.Fail("Error getting class data.");

        }

        public async Task<OperationResult<List<PeerReviewLessonData>>> GetToDoQuestionsAsync()
        {
            if (_isToDoQuestionsValid == false)
            {
                PrintCacheMiss();
                var ulr = ApiHelper.GetStudentToDoQuestions(this.token, this.courseId, this.role);
                var result = await GetFromApi(ulr);
                if (result.IsSuccessStatusCode)
                {
                    var response = await result.Content.ReadAsStringAsync();
                    // convert response to PeerReviewClassData
                    try
                    {
                        var peerReviewClass = JsonConvert.DeserializeObject<List<PeerReviewLessonData>>(response);
                        if (peerReviewClass != null)
                        {
                            this._todoQuestions = peerReviewClass;
                            return OperationResult<List<PeerReviewLessonData>>.Ok(_todoQuestions);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Write(ex.Message);
                    }
                }
            }
            else
            {
                PrintCacheHit();
                return OperationResult<List<PeerReviewLessonData>>.Ok(this._todoQuestions);
            }

            return OperationResult<List<PeerReviewLessonData>>.Fail("Error getting todo questions.");

        }

        public async Task<OperationResult<PeerReviewAnswerForFeedbackData>> GetFeedbackAsync(int lessonID)
        {

            var ulr = ApiHelper.GetFeedback(this.token, lessonID, this.role);
            var result = await GetFromApi(ulr);
            if (result.IsSuccessStatusCode)
            {
                var response = await result.Content.ReadAsStringAsync();
                // convert response to PeerReviewClassData
                try
                {
                    var peerReviewClass = JsonConvert.DeserializeObject<PeerReviewAnswerForFeedbackData>(response);
                    if (peerReviewClass != null)
                    {
                        return OperationResult<PeerReviewAnswerForFeedbackData>.Ok(peerReviewClass);
                    }
                }
                catch (Exception ex)
                {
                    Console.Write(ex.Message);
                }
            }


            return OperationResult<PeerReviewAnswerForFeedbackData>.Fail("Error getting feedback.");

        }

        internal async Task<OperationResult<List<PeerReviewAnswerData>>> GetGradesAsync(int lesson_id)
        {
            var ulr = ApiHelper.GetAnswerStudentsDone(this.token, lesson_id, this.role);
            var result = await GetFromApi(ulr);
            if (result.IsSuccessStatusCode)
            {
                var response = await result.Content.ReadAsStringAsync();               
                try
                {
                    List<PeerReviewAnswerData>? peerReviewClass = JsonConvert.DeserializeObject<List<PeerReviewAnswerData>>(response);
                    if (peerReviewClass != null)
                    {
                        return OperationResult<List<PeerReviewAnswerData>>.Ok(peerReviewClass);
                    }
                }
                catch (Exception ex)
                {
                    Console.Write(ex.Message);
                }
            }
            return OperationResult<List<PeerReviewAnswerData>>.Fail("Error getting feedback.");
        }
    }

    public class TeacherLocalCache : LocalCache {

        private List<PeerReviewSummaryLessonTeacherData> _summary = null;
        
        private PeerReviewClassData _classData = null;

        private List<QuestionToMarkTeacherData> _questionToMarkTeachers = null;

        private bool _isSummaryValid { get => _summary != null; }
        private bool _isClassDataValid { get => _classData != null; }

        private bool _isQuestionToMarkTeachersValid { get => _questionToMarkTeachers != null; }


        public void ResetCache()
        {
            this._summary = null;
            this._classData = null;
        }

        public TeacherLocalCache(int courseId, Guid token, PeerReviewRole role, HttpClient client) : base(courseId, token, role, client)
        {

        }

        public async Task<OperationResult<List<PeerReviewSummaryLessonTeacherData>>> GetTeacherLessonSummaryDataAsync()
        {
            if (_isSummaryValid == false)
            {
                PrintCacheMiss();
                var ulr = ApiHelper.GetTeacherLessonsSummary(this.token, this.courseId, this.role);
                var result = await GetFromApi(ulr);
                if (result.IsSuccessStatusCode)
                {
                    var response = await result.Content.ReadAsStringAsync();
                    // convert response to PeerReviewClassData
                    var summary = JsonConvert.DeserializeObject<List<PeerReviewSummaryLessonTeacherData>>(response);
                    if (summary != null)
                    {
                        this._summary = summary;
                        return OperationResult<List<PeerReviewSummaryLessonTeacherData>>.Ok(summary);
                    }
                }
            }
            else
            {
                PrintCacheHit();
                return OperationResult<List<PeerReviewSummaryLessonTeacherData>>.Ok(this._summary);
            }

            return OperationResult<List<PeerReviewSummaryLessonTeacherData>>.Fail("Error getting class data.");

        }

        public async Task<OperationResult<List<QuestionToMarkTeacherData>>> GetQuestionsToMark(int lessonId)
        {
            if (_isQuestionToMarkTeachersValid == false)
            {
                PrintCacheMiss();
                var ulr = ApiHelper.GetTeacherQuestionsToMark(this.token, lessonId, this.role);
                var result = await GetFromApi(ulr);
                if (result.IsSuccessStatusCode)
                {
                    var response = await result.Content.ReadAsStringAsync();
                    // convert response to PeerReviewClassData
                    var questionToMarkTeachers = JsonConvert.DeserializeObject<List<QuestionToMarkTeacherData>>(response);
                    if (questionToMarkTeachers != null)
                    {
                        this._questionToMarkTeachers = questionToMarkTeachers;
                        return OperationResult<List<QuestionToMarkTeacherData>>.Ok(questionToMarkTeachers);
                    }
                }
            }
            else
            {
                PrintCacheHit();
                return OperationResult<List<QuestionToMarkTeacherData>>.Ok(this._questionToMarkTeachers);
            }

            return OperationResult<List<QuestionToMarkTeacherData>>.Fail("Error getting class data.");

        }

        public async Task<OperationResult<PeerReviewClassData>> GetPeerReviewClassDataAsync()
        {
            if (_isClassDataValid == false)
            {
                PrintCacheMiss();
                var ulr = ApiHelper.GetClass(this.token, this.courseId, this.role);
                var result = await GetFromApi(ulr);
                if (result.IsSuccessStatusCode)
                {
                    var response = await result.Content.ReadAsStringAsync();
                    // convert response to PeerReviewClassData
                    var peerReviewClass = JsonConvert.DeserializeObject<PeerReviewClassData>(response);
                    if (peerReviewClass != null)
                    {
                        this._classData = peerReviewClass;
                        return OperationResult<PeerReviewClassData>.Ok(_classData);
                    }
                }
            }
            else
            {
                PrintCacheHit();
                return OperationResult<PeerReviewClassData>.Ok(this._classData);
            }

            return OperationResult<PeerReviewClassData>.Fail("Error getting class data.");

        }

        public async Task<OperationResult<IEnumerable<PeerReviewUserData>>> GetPeerReviewStudentsAsync()
        {
            var url = ApiHelper.GetStudents(this.token, this.courseId, this.role);
            var result = await GetFromApi(url);
            if (result.IsSuccessStatusCode)
            {
                var response = await result.Content.ReadAsStringAsync();
                var students = JsonConvert.DeserializeObject<IEnumerable<PeerReviewUserData>>(response);
                if (students != null)
                {
                    return OperationResult<IEnumerable<PeerReviewUserData>>.Ok(students);
                }
            }
            return OperationResult<IEnumerable<PeerReviewUserData>>.Fail("Error getting students.");
        }
        public async Task<OperationResult<IEnumerable<PeerReviewQuestionData>>> GetQuestionsToReviewAsync()
        {
            var url = ApiHelper.GetTeacherQuestionsToReview(this.token, this.courseId, this.role);
            var result = await GetFromApi(url);
            if (result.IsSuccessStatusCode)
            {
                var response = await result.Content.ReadAsStringAsync();
                var json = JsonConvert.DeserializeObject<IEnumerable<PeerReviewQuestionData>>(response);
                if (json != null)
                {
                    return OperationResult<IEnumerable<PeerReviewQuestionData>>.Ok(json);
                }
            }
            return OperationResult<IEnumerable<PeerReviewQuestionData>>.Fail("Error getting data.");
        }




    }

}

﻿using Newtonsoft.Json;
using NLog;
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
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static void PrintCacheMiss() { } // Console.WriteLine(); AnsiConsole.MarkupLine($"[lightslategrey]Cache Miss[/]");
        public static void PrintCacheHit() { } // Console.WriteLine(); AnsiConsole.MarkupLine($"[lightslateblue]Cache Hit[/]");

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
            if (getResponse.IsSuccessStatusCode == false)
            {
                var errorContent = await getResponse.Content.ReadAsStringAsync();
                logger.Error(new Exception("Error in GetFromApi()"), errorContent);                
            }

            if(Program.SAVE_JSON_REPLAY_SERVER)
            {
                var json = await getResponse.Content.ReadAsStringAsync();
                logger.Info(relativePath + "\n" + json);
            }

            return getResponse;
        }

        public bool Post(object item, string url)
        {
            var json = JsonConvert.SerializeObject(item);
            var data = new StringContent(json, Encoding.UTF8, "application/json");
            var response = this.client.PostAsync(url, data);

            if (Program.SAVE_JSON_REPLAY_SERVER)
            {
                logger.Info(url + "\n" + json);
            }

            return response.Result.IsSuccessStatusCode;
        }

    }

    public class StudentLocalCache : LocalCache
    {
        private List<PeerReviewSummaryLessonStudentData> _summary = null;

        private List<PeerReviewLessonData> _todoQuestions = null;

        private List<PeerReviewLessonData> _dashboard = null;


        private bool _isSummaryValid { get => _summary != null; }
        private bool _isToDoQuestionsValid { get => _todoQuestions != null; }
        private bool _isDashboardValid { get => _dashboard != null; }



        public void ResetCache()
        {
            this._summary = null;
            this._todoQuestions = null;
            this._dashboard = null;
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

        internal async Task<OperationResult<List<PeerReviewLessonData>>> GetDashboardAsync()
        {            
            if (_isDashboardValid == true)
            {
                PrintCacheHit();
                return OperationResult<List<PeerReviewLessonData>>.Ok(this._dashboard);
            }

            var ulr = ApiHelper.GetAllGrade(this.token, this.role);
            var result = await GetFromApi(ulr);
            if (result.IsSuccessStatusCode)
            {
                try
                {
                    var response = await result.Content.ReadAsStringAsync();
                    // BUG FIX - the response is a string, not a list of PeerReviewLessonData ???
                    //string correctedJson = JsonConvert.DeserializeObject<string>(response.ToString());
                    var item = JsonConvert.DeserializeObject<List<PeerReviewLessonData>>(response.ToString());
                    if (item != null)
                    {
                        this._dashboard = item;
                        return OperationResult<List<PeerReviewLessonData>>.Ok(item);
                    }
                }
                catch (Exception ex)
                {
                    Console.Write(ex.Message);
                }
            }
            return OperationResult<List<PeerReviewLessonData>>.Fail("Error getting feedback.");
        }



    }


    public enum CacheItemType
    {
        Summary,
        ClassData,
        QuestionToMarkTeachers,
        CorrectAnswers
    }

    public class TeacherLocalCache : LocalCache {

        private List<PeerReviewSummaryLessonTeacherData> _summary = null;
        
        private PeerReviewClassData _classData = null;

        private List<QuestionToMarkTeacherData> _questionToMarkTeachers = null;

        private IEnumerable<PeerReviewQuestionData> _correctAnswers = null;

        private bool _isSummaryValid { get => _summary != null; }
        private bool _isClassDataValid { get => _classData != null; }

        private bool _isQuestionToMarkTeachersValid { get => _questionToMarkTeachers != null; }

        private bool _isCorrectAnswersValid { get => _correctAnswers != null; }


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
            var url = ApiHelper.GetCorrectAnswerToReview(this.token, this.courseId, this.role);
            var result = await GetFromApi(url);
            if (result.IsSuccessStatusCode)
            {
                var response = await result.Content.ReadAsStringAsync();
                var json = JsonConvert.DeserializeObject<IEnumerable<PeerReviewQuestionData>>(response);
                if (json != null)
                {
                    this._correctAnswers = json;
                    return OperationResult<IEnumerable<PeerReviewQuestionData>>.Ok(json);
                }
            }
            return OperationResult<IEnumerable<PeerReviewQuestionData>>.Fail("Error getting data.");
        }

        public bool RemoveItemFromCache(CacheItemType itemType, int lessonId)
        {
            switch(itemType)
            {
                case CacheItemType.Summary:
                    this._summary = null;
                    return true;
                case CacheItemType.ClassData:
                    this._classData = null;
                    return true;
                case CacheItemType.QuestionToMarkTeachers:
                    this._questionToMarkTeachers = null;
                    return true;
                case CacheItemType.CorrectAnswers:
                    this._correctAnswers = this._correctAnswers.Where(x => x.id != lessonId);
                    return true;
                default:
                    return false;
            }
        }

    }

}

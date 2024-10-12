namespace PeerReviewClient
{
    public class TeacherMenu : BaseMenu
    {
        private TeacherLocalCache _localCache { get; set; }

        public TeacherMenu(MenuInitOptionsData options) : base(options)
        {
            this._localCache = new TeacherLocalCache(courseId, token, role, options.client);
        }

        public override async Task InitMenu()
        {

        }

        public override List<MenuOption> GetMenuOptions()
        {
            var menu = new List<MenuOption>();
            menu.Add(new MenuOption(this.localization.GetText(TranslateKey.SHOW_LESSONS), 1, ShowTeacherLessons));
            menu.Add(new MenuOption(this.localization.GetText(TranslateKey.ADD_LESSON), 2, AddLesson));
            menu.Add(new MenuOption(this.localization.GetText(TranslateKey.ADD_QUESTIONS_TO_LESSON), 3, AddQuestions));
            menu.Add(new MenuOption(this.localization.GetText(TranslateKey.MARK_QUESTION), 4, MarkQuestion));
            menu.Add(new MenuOption(this.localization.GetText(TranslateKey.QUESTIONS_TO_REVIEW), 5, QuestionsToReview));

            if (this.saveCredentials)
                menu.Add(new MenuOption(this.localization.GetText(TranslateKey.DELETE_CREDENTIALS), 11, DeleteCredentials));
            menu.Add(new MenuOption("Exit", 0, null));
            return menu;
        }

        private async Task QuestionsToReview()
        {
            DisplayTitle("Revisiona domande corrette");

            //var courseId = this.courseId;
            var courseId = -1;
            var fetchData = await _localCache.GetQuestionsToReviewAsync();
            PeerReviewQuestionData? selectedLesson;
            if (fetchData.Result == ExecutionStatus.Done)
            {
                var value = fetchData.Value;
                if (value != null && value.Count() > 0)
                {
                    DisplayMessage(" ");
                    var table = new TableHelper(this.studentsOptions, this.localization);
                    table.PrintQuestionsToReview(value);

                    var lessonId = -1;
                    while (true)
                    {
                        var lessonIdStr = PromptForInlineInput("Lesson id: ");
                        if (lessonIdStr.Result != ExecutionStatus.Done)
                        {
                            return;
                        }
                        if (int.TryParse(lessonIdStr.Value, out lessonId))
                        {
                            selectedLesson = value.FirstOrDefault(l => l.id == lessonId);
                            if (selectedLesson != null)
                            {
                                break;
                            }
                            else
                            {
                                PrintError("Lesson not found. Please try again.");
                            }
                        }
                        else
                        {
                            PrintError("Invalid input. Please try again.");
                        }
                    }

                    Console.WriteLine(" ");
                    Console.WriteLine(selectedLesson.answer);

                    var result = PromptForInlineInput("Mark as correct? (y/n): ");
                    if (result.Result != ExecutionStatus.Done)
                    {
                        return;
                    }
                    var url = ApiHelper.PostCorrectAnswerToReview();
                    var itemToSend = new CorrectAnswerToReviewJsonData()
                    {
                        token = this.token,
                        answer = "",
                        course_class_id = this.courseId,
                        lesson_id = lessonId,
                        role = PeerReviewRole.teacher,
                        website = 8,
                        is_answer_edit = true,
                    };

                    if (result.Value.ToLower() == "y")
                    {

                    }
                    else
                    {
                        var feedback = PromptForInput("New answer: ");
                        if (feedback.Result != ExecutionStatus.Done)
                        {
                            return;
                        }
                        itemToSend.answer = feedback.Value;
                    }

                    if (_localCache.Post(itemToSend, url))
                    {
                        PrintSuccess("Answer marked as correct.");
                        _localCache.RemoveItemFromCache(CacheItemType.CorrectAnswers, lessonId);
                    }
                    else
                    {
                        PrintError("Error marking answer as correct.");
                    }
                }
                else
                {
                    PrintError("No revisionare domande corrette found.");
                }
            }
        }
        private async Task MarkQuestion()
        {
            DisplayTitle("Correggi una domanda");

            var lessonId = -1;
            PeerReviewLessonData lessonSelected = null;
            while (true)
            {
                var tLessonIDResult = PromptForInlineInput("Lesson id: ");
                if (tLessonIDResult.Result != ExecutionStatus.Done)
                {
                    return;
                }

                if (int.TryParse(tLessonIDResult.Value, out lessonId) == false)
                {
                    DisplayMessage("Invalid input. Please try again.");
                    continue;
                }
                // Check if lesson exists
                var fetchLessonsData = await _localCache.GetPeerReviewClassDataAsync();
                if (fetchLessonsData.Result == ExecutionStatus.Done)
                {
                    lessonSelected = fetchLessonsData.Value.lessons.FirstOrDefault(l => l.id == lessonId);
                    if (lessonSelected != null)
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

            var fetchDataQuestionsToMark = await _localCache.GetQuestionsToMark(lessonId);
            if (fetchDataQuestionsToMark.Result == ExecutionStatus.Done)
            {
                var tableHelper = new TableHelper(this.studentsOptions, this.localization);
                tableHelper.PrintQuestionsToMark(fetchDataQuestionsToMark.Value);
            }

            QuestionToMarkTeacherData questionToMark;
            var questionIdToMark = -1;
            while (true)
            {
                var tQuestionIDResult = PromptForInlineInput("Question id: ");
                if (tQuestionIDResult.Result != ExecutionStatus.Done)
                {
                    return;
                }

                if (int.TryParse(tQuestionIDResult.Value, out questionIdToMark) == false)
                {
                    PrintError("Invalid input. Please try again.");
                    continue;
                }

                questionToMark = fetchDataQuestionsToMark.Value.FirstOrDefault(q => q.answer_id == questionIdToMark);
                // Check if question exists
                if (questionToMark != null)
                {
                    break;
                }
                else
                {
                    PrintError("Question not found. Please try again.");
                }
            }

            var GetFeedbackData = GetFeedback(
                lessonId,
                new PeerReviewAnswerForFeedbackData()
                {
                    id = questionToMark.answer_id,
                    answer_text = questionToMark.answer_text,
                    question = questionToMark.question_text
                });

            if (GetFeedbackData.Result != ExecutionStatus.Done)
            {
                return;
            }

            if (SentFeedback(_localCache, GetFeedbackData.Value))
            {
                _localCache.ResetCache();
            }
        }

        private async Task AddLesson()
        {
            DisplayTitle("Adding Lesson");

            var title = PromptForInput("Title: ");
            if (title.Result != ExecutionStatus.Done)
            {
                return;
            }

            var fistDeadlineResult = PromptForInput("First Deadline in h (48 default): ");
            if (fistDeadlineResult.Result != ExecutionStatus.Done)
            {
                return;
            }
            var fistDeadline = fistDeadlineResult.Value;
            if (fistDeadline == string.Empty)
            {
                fistDeadline = "48";
            }

            var secondDeadlineResult = PromptForInput("Second Deadline in h (120 default): ");
            if (secondDeadlineResult.Result != ExecutionStatus.Done)
            {
                return;
            }

            var secondDeadline = secondDeadlineResult.Value;
            if (secondDeadline == string.Empty)
            {
                secondDeadline = "120";
            }

            var firstDeadlineDate = DateTime.Now.AddHours(double.Parse(fistDeadline));
            var secondDeadlineDate = DateTime.Now.AddHours(double.Parse(secondDeadline));

            var lessonData = new PeerReviewLessonJsonData()
            {
                title = title.Value,
                content_html = "",
                first_deadline = firstDeadlineDate,
                second_deadline = secondDeadlineDate,
                created_at = DateTime.Now,
                course_class_id = this.courseId,
                role = PeerReviewRole.teacher,
                token = this.token,
                website = 8,
            };

            var postResult = _localCache.Post(lessonData, ApiHelper.PostLessons());

            if (postResult)
            {
                PrintSuccess("Lesson added successfully.");
                _localCache.ResetCache();
            }
            else
            {
                PrintError("Error adding lesson.");
            }

        }
        private async Task ShowTeacherLessons()
        {
            DisplayTitle("Showing Lessons");

            var fetchData = await _localCache.GetTeacherLessonSummaryDataAsync();

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
                    PrintError("No lessons found.");
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
                if (tLessonID.Result != ExecutionStatus.Done)
                {
                    return;
                }

                if (int.TryParse(tLessonID.Value, out lessonId) == false)
                {
                    PrintError("Invalid input. Please try again.");
                    continue;
                }

                // Check if lesson exists
                var fetchData = await _localCache.GetPeerReviewClassDataAsync();
                if (fetchData.Result == ExecutionStatus.Done)
                {
                    if (fetchData.Value.lessons.Any(l => l.id == lessonId))
                    {
                        break;
                    }
                    else
                    {
                        PrintError("Lesson not found. Please try again.");
                    }
                }
                else
                {
                    DisplayMessage("Error getting class data...");
                }
            }

            DisplayMessage(localization.GetText(TranslateKey.INSERT_STUDENT_ABSENCE));
            var fetchStudents = await _localCache.GetPeerReviewStudentsAsync();
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
                PrintError("Errore nel recupero degli studenti");
                return;
            }


            DisplayMessage("Aggiungi le domande. Invio per fermarsi");

            List<PeerReviewQuestionJsonData> questions = new List<PeerReviewQuestionJsonData>();
            while (true)
            {
                var question = PromptForInlineInput($"Domanda {questions.Count + 1}: ");
                if (question.Result != ExecutionStatus.Done)
                {
                    return;
                }

                if (string.IsNullOrEmpty(question.Value))
                {
                    if (questions.Count == 0)
                    {
                        PrintError("Inserire almeno una domanda.");
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    questions.Add(new PeerReviewQuestionJsonData() { question_text = question.Value });
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
            var postResult = _localCache.Post(itemToAdd, ApiHelper.PostQuestion());

            if (postResult)
            {
                PrintSuccess("Questions added successfully.");
                _localCache.ResetCache();
            }
            else
            {
                PrintError("Error adding lesson.");
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
                if (userInput.Result != ExecutionStatus.Done)
                {
                    return OperationResult<IEnumerable<Student>>.Fail("EscapeClick");
                }
                if (string.IsNullOrEmpty(userInput.Value))
                {
                    break;
                }

                var listId = new List<int>();
                var listIdToConvert = userInput.Value.Split(' ');

                foreach (var item in listIdToConvert)
                {
                    if (int.TryParse(item, out int tempID))
                    {
                        listId.Add(tempID);
                        Menu.DisplayMessage($"Add {item}");
                    }
                    else
                    {
                        Menu.PrintError($"Failed to convert {item}");
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
                        Menu.PrintError("Student not found. Please try again.");
                    }
                }

                table.PrintRegister(this.Students);

            }

            return OperationResult<IEnumerable<Student>>.Ok(this.Students);
        }

    }
}

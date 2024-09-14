namespace PeerReviewClient
{
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
            menu.Add(new MenuOption(this.localization.GetText(TranslateKey.MARK_QUESTION), 4, MarkQuestion));



            if (this.saveCredentials)
                menu.Add(new MenuOption(this.localization.GetText(TranslateKey.DELETE_CREDENTIALS), 11, DeleteCredentials));
            menu.Add(new MenuOption("Exit", 0, null));
            return menu;
        }

        private async Task MarkQuestion()
        {
            DisplayTitle("Correggi una domanda");
                      
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

            var fetchDataQuestionsToMark = await localCache.GetQuestionsToMark(lessonId);
            if (fetchDataQuestionsToMark.Result == ExecutionStatus.Done) {
                var tableHelper = new TableHelper(this.studentsOptions, this.localization);
                tableHelper.PrintQuestionsToMark(fetchDataQuestionsToMark.Value);
            }
            
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

            var postResult = localCache.Post(lessonData, ApiHelper.PostLessons());

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
            var postResult = localCache.Post(itemToAdd, ApiHelper.PostQuestion());

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

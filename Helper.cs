﻿using Spectre.Console;
using System.Globalization;
using System.Text;

namespace PeerReviewClient
{

    /// <summary>
    /// Helper class to print tables
    /// </summary>
    public class TableHelper
    {
        AssignQuestionsToStudentsOptions _studentsOptions;
        Localization _localization;

        public TableHelper(AssignQuestionsToStudentsOptions studentsOptions, Localization localization)
        {
            this._studentsOptions = studentsOptions;
            this._localization = localization;
        }

        private static string GetColor(DateTime? startDate, DateTime? endDate, int count, int max)
        {
            DateTime now = DateTime.Now;

            if (startDate.HasValue && startDate.Value > now)
            {
                return "[gray]";
            }
            else if (endDate.HasValue && endDate.Value < now)
            {
                if (count >= max)
                {
                    return "[lightgreen]";
                }
                else
                {
                    return "[red]";
                }
            }
            else if (startDate.HasValue && startDate.Value <= now && (endDate == null || endDate.Value >= now))
            {
                if (count >= max)
                {
                    return "[green]";
                }
                else
                {
                    return "[yellow]";
                }
            }

            return "[white]";
        }

        public void PrintStudentLessons(List<PeerReviewSummaryLessonStudentData> peerReviewClass)
        {

            var listTable = new List<StudentLessonsTableData>();

            foreach (var lesson in peerReviewClass)
            {
                var dateChecker = new DateChecker(DateTime.Now, lesson.first_deadline, lesson.second_deadline);
                var timeInterval = dateChecker.GetTimeInterval();

                var firstDeadlineLabel = timeInterval == TimeInterval.BeforeFirstDeadline
                    ? Markup.Escape(_localization.GetText(TranslateKey.ACTIVE_LESSON_LABEL))
                    : "";

                var secondDeadlineLabel = timeInterval == TimeInterval.BetweenDeadlines
                    ? Markup.Escape(_localization.GetText(TranslateKey.ACTIVE_FEEDBACK_LABEL))
                    : "";

                var countQuestionsMade = GetColor(lesson.created_at, lesson.first_deadline, lesson.count_questions_made, this._studentsOptions.QuestionsToAnswer) + lesson.count_questions_made + "/" + this._studentsOptions.QuestionsToAnswer + "[/]";

                var countFeedbackMade = GetColor(lesson.first_deadline, lesson.second_deadline, lesson.count_feedback_made, this._studentsOptions.FeedbacksToGive) + lesson.count_feedback_made + "/" + this._studentsOptions.FeedbacksToGive + "[/]";

                listTable.Add(new StudentLessonsTableData
                {
                    id = lesson.id,
                    title = lesson.title,
                    created_at = lesson.created_at.ToString("dd/MM/yyyy HH:mm:ss"),
                    first_deadline = (lesson.first_deadline?.ToString("dd/MM/yyyy HH:mm:ss") ?? "") + firstDeadlineLabel,
                    count_questions_made = countQuestionsMade,
                    second_deadline = (lesson.second_deadline?.ToString("dd/MM/yyyy HH:mm:ss") ?? "") + secondDeadlineLabel,
                    count_feedback_made = countFeedbackMade
                });

            }


            var table = new Table();
            table.Border = TableBorder.Horizontal;
            table.AddColumns("ID", "Title", "Created At", "First Deadline", "Questions made", "Second Deadline", "Feedback made");

            var count = 0;
            foreach (var item in listTable)
            {
                table.AddRow(
                    item.id.ToString(),
                    item.title,
                    item.created_at,
                    item.first_deadline,
                    item.count_questions_made.ToString(),
                    item.second_deadline,
                    item.count_feedback_made.ToString());

                if (count != listTable.Count - 1)
                {
                    table.AddEmptyRow();
                }
                else
                {
                    var t = "";
                }
                count++;
            }

            table.Expand();
            table.Columns[0].Centered();

            AnsiConsole.Write(table);

        }

        internal void PrintTeacherLessons(List<PeerReviewSummaryLessonTeacherData> peerReviewClass)
        {
            var listTable = new List<TeacherLessonTableData>();

            foreach (var lesson in peerReviewClass)
            {
                var dateChecker = new DateChecker(DateTime.Now, lesson.first_deadline, lesson.second_deadline);
                var timeInterval = dateChecker.GetTimeInterval();

                var firstDeadlineLabel = timeInterval == TimeInterval.BeforeFirstDeadline
                    ? Markup.Escape(_localization.GetText(TranslateKey.ACTIVE_LESSON_LABEL))
                    : "";

                var secondDeadlineLabel = timeInterval == TimeInterval.BetweenDeadlines 
                    ? Markup.Escape(_localization.GetText(TranslateKey.ACTIVE_FEEDBACK_LABEL))
                    : "";

                var nStudents = (int)lesson.count_questions_made / this._studentsOptions.QuestionsToAnswer;
                var nFeedbacks = (int)nStudents * this._studentsOptions.FeedbacksToGive;

                listTable.Add(new TeacherLessonTableData
                {
                    id = lesson.id.ToString(),
                    title = lesson.title,
                    created_at = (lesson.created_at?.ToString("dd/MM/yyyy HH:mm") ?? ""),
                    first_deadline = (lesson.first_deadline?.ToString("dd/MM/yyyy HH:mm") ?? "") + firstDeadlineLabel,
                    second_deadline = (lesson.second_deadline?.ToString("dd/MM/yyyy HH:mm") ?? "") + secondDeadlineLabel,
                    count_questions = lesson.count_questions.ToString(),
                    answered_questions_range = lesson.total_answered_questions + "/" + lesson.count_questions_made.ToString(),
                    count_feedback_made_range = lesson.count_feedback_made.ToString() + "/" + nFeedbacks,
                });
            }

            var table = new Table();
            table.Border = TableBorder.Horizontal;

            table.AddColumns("id", "title", "n questions", "date", "first deadline", "second deadline", "questions", "feedback");

            var count = 0;
            foreach (var item in listTable)
            {
                table.AddRow(
                    item.id,
                    item.title,
                    item.count_questions,
                    item.created_at,
                    item.first_deadline,
                    item.second_deadline,
                    item.answered_questions_range,
                    item.count_feedback_made_range
                    );

                if (count != listTable.Count - 1)
                {
                    table.AddEmptyRow();
                }
                count++;
            }

            table.Expand();
            table.Columns[0].Centered();

            AnsiConsole.Write(table);
        }

        public void PrintRegister(IEnumerable<Student> students)
        {

            var table = new Table();
            table.Border = TableBorder.Horizontal;
            var name = _localization.GetText(TranslateKey.NAME);
            var isPresent = _localization.GetText(TranslateKey.IS_PRESENT);
            table.AddColumns("id", name, isPresent);

            var present = "[green]" + _localization.GetText(TranslateKey.PRESENT) + "[/]";
            var absent = "[red]" + _localization.GetText(TranslateKey.ABSENT) + "[/]";

            foreach (var student in students)
            {
                table.AddRow(student.registerNumber.ToString(), student.name, student.isPresent ? present : absent);
            }

            table.Expand();
            table.Columns[0].Centered();

            AnsiConsole.Write(table);

        }

        internal void PrintGrades(List<PeerReviewAnswerData> list)
        {
            foreach (var item in list)
            {
                Console.WriteLine(item.id + ") " + item.question_text);
                Console.WriteLine(item.answer_text);

                var table = new Table();
                table.Border = TableBorder.Horizontal;
                table.AddColumns("#", "feedback", "missing elements", "grade");
                var i = 0;
                decimal avarage = 0;
                foreach (var feedback in item.feedbacks)
                {
                    feedback.feedback_text = feedback.feedback_text.Replace("\n", " ");
                    feedback.missing_elements = feedback.missing_elements.Replace("\n", " ");
                    table.AddRow(i.ToString(), feedback.feedback_text, feedback.missing_elements, feedback.grade.ToString());
                    avarage += feedback.grade;
                    i++;
                }

                if (i > 0)
                {
                    avarage = avarage / i;
                    table.AddRow("AVG", "-", "-", avarage.ToString());
                }

                table.Expand();
                table.Columns[0].Centered();
                AnsiConsole.Write(table);
                Console.WriteLine();
            }
        }

        public void PrintQuestions(List<PeerReviewLessonData> list)
        {
            var table = new Table();
            table.Border = TableBorder.Horizontal;
            table.AddColumns("ID", "Question", "Title", "Created At", "Deadline");
            foreach (var lesson in list)
            {
                foreach (var question in lesson.lesson_questions)
                {
                    var dateFirstDeadline = lesson.first_deadline == null ? " " : lesson.first_deadline.Value.ToString("dd/MM/yyyy HH:mm:ss");

                    table.AddRow(
                                question.id.ToString(),
                                question.question_text,
                                Helper.RemoveDiacritics(lesson.title),
                                lesson.created_at.ToString("dd/MM/yyyy HH:mm:ss"),
                                dateFirstDeadline);
                }
            }

            table.Expand();
            table.Columns[0].Centered();
            AnsiConsole.Write(table);
            Console.WriteLine();
        }
    }
    public static class Helper
    {
        public static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }
            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

    }

    /// <summary>
    /// Helper class to get the API paths
    /// </summary>
    public static class ApiHelper
    {
        public static string GetApiBase(bool debugMode = false) { 
        
            if(debugMode)
                return "https://localhost:44391/api/v1/";
            else
                return "https://www.apibaobab.com/api/v1/";

        }

        public static string GetPeerReviewClass(Guid guid, int classId, PeerReviewRole user_type, int website = 8)
        {
            return "PeerReview/Class/" + website + "/" + (int)user_type + "/" + classId + "/" + guid.ToString();
        }

        internal static string GetPeerReviewStudents(Guid token, int courseId, PeerReviewRole role, int website = 8)
        {

            return "PeerReview/Students/" + website + "/" + (int)role + "/" + courseId + "/" + token.ToString();
        }

        internal static string GetPeerReviewToDoQuestions(Guid token, int courseId, PeerReviewRole role, int website = 8)
        {
            return "PeerReview/Students/ToDoQuestions/" + website + "/" + (int)role + "/" + courseId + "/" + token.ToString();
        }

        internal static string GetPeerReviewFeedback(Guid token, int lesson_id, PeerReviewRole role, int website = 8)
        {
            return "PeerReview/Feedback/" + website + "/" + (int)role + "/" + lesson_id + "/" + token.ToString();
        }

        internal static string PostPeerReviewFeedback()
        {
            return "PeerReview/Feedback";
        }

        internal static string PostPeerReviewRole()
        {
            return "PeerReview/Role";
        }

        internal static string PostLogin()
        {
            return "Login";
        }

        internal static string GetPeerReviewStudentLessonsSummary(Guid token, int courseId, PeerReviewRole role, int website = 8)
        {
            return "PeerReview/Lessons/Summary/Student/" + website + "/" + (int)role + "/" + courseId + "/" + token.ToString();
        }

        internal static string GetPeerReviewTeacherLessonsSummary(Guid token, int courseId, PeerReviewRole role, int website = 8)
        {
            return "PeerReview/Lessons/Summary/Teacher/" + website + "/" + (int)role + "/" + courseId + "/" + token.ToString();
        }

        internal static string GetPeerReviewAnswerStudentsDone(Guid token, int lesson_id, PeerReviewRole role, int website = 8)
        {
            return "PeerReview/Answer/Lesson/" + website + "/" + (int)role + "/" + lesson_id + "/" + token.ToString();
        }

        internal static string PostPeerReviewEnroll()
        {
            return "PeerReview/Enroll";
        }

        internal static string PostPeerReviewLessons()
        {
            return "PeerReview/Lessons";
        }

        internal static string PostPeerReviewQuestion()
        {
            return "PeerReview/Question";
        }

    }
    public class DateChecker
    {
        private DateTime currentTime;
        private DateTime? firstDeadline;
        private DateTime? secondDeadline;

        public DateChecker(DateTime currentTime, DateTime? firstDeadline, DateTime? secondDeadline)
        {
            this.currentTime = currentTime;
            this.firstDeadline = firstDeadline;
            this.secondDeadline = secondDeadline;
        }

        public TimeInterval GetTimeInterval()
        {
            if (currentTime < firstDeadline)
            {
                return TimeInterval.BeforeFirstDeadline;
            }
            else if (currentTime >= firstDeadline && currentTime <= secondDeadline)
            {
                return TimeInterval.BetweenDeadlines;
            }
            else
            {
                return TimeInterval.AfterSecondDeadline;
            }
        }
    }


    public class VersionChecker
    {
        private string currentVersion;
        private string newVersion;

        public VersionChecker(string currentVersion, string newVersion)
        {
            this.currentVersion = currentVersion;
            this.newVersion = newVersion;
        }

        public VersionComparisonResult CompareVersions()
        {
            var currentVersionParts = ParseVersion(currentVersion);
            var newVersionParts = ParseVersion(newVersion);

            if (AreSameVersion(currentVersionParts, newVersionParts))
            {
                return VersionComparisonResult.SameVersion;
            }
            else if (IsIncompatible(currentVersionParts, newVersionParts))
            {
                return VersionComparisonResult.Incompatible;
            }
            else
            {
                return VersionComparisonResult.UpdateRecommended;
            }
        }

        private int[] ParseVersion(string version)
        {
            var versionParts = version.Split('.');
            return Array.ConvertAll(versionParts, int.Parse);
        }

        private bool AreSameVersion(int[] current, int[] newVersion)
        {
            return current[0] == newVersion[0] && current[1] == newVersion[1] && current[2] == newVersion[2];
        }

        private bool IsIncompatible(int[] current, int[] newVersion)
        {
            // If the major or minor version differs, it's incompatible.
            return current[0] != newVersion[0] || current[1] != newVersion[1];
        }
    }

}

using KSAGrinder.Components;

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace KSAGrinder.Statics
{
    internal static class DataManager
    {
        public static DataSet Data { get; private set; }

        public static void SetData(DataSet data)
        {
            Data = data;
            InitializeClassDictionary();
            InitializeLectureList();
        }

        /// <summary>
        /// correspond a code-grade tuple to a list of classes
        /// </summary>
        private static readonly Dictionary<(string, int), List<Class>> _classDict = new();

        private static readonly List<Lecture> _lectures = new();

        public static IEnumerable<Lecture> GetLectures()
        {
            foreach (Lecture lecture in _lectures)
                yield return lecture;
        }

        public static Lecture GetLecture(string code, int grade) => _lectures.Find(x => x.Code == code && x.Grade == grade);

        public static Class GetClass(string code, int grade, int number) => _classDict[(code, grade)].Find(cls => cls.Number == number);

        public static bool LectureExists(string code, int grade) => _classDict.ContainsKey((code, grade));

        public static bool StudentExists(string id) => Data.Tables["Student"].Rows.Find(id) is not null;

        public static bool ClassExists(string code, int grade, int number) => LectureExists(code, grade) && 1 <= number && number <= _classDict[(code, grade)].Count;

        public static string GetNameOfLectureFromCode(string code) => _lectures.Find(lecture => lecture.Code == code).Name;

        public static DataRow GetClassRow(string code, int number)
        {
            DataTable tClass = Data.Tables["Class"];
            DataColumn ccCode = tClass.Columns["Code"];
            DataColumn ccNumber = tClass.Columns["Number"];

            DataRow classRow = null;
            foreach (DataRow row in tClass.Rows)
            {
                if ((string)row[ccCode] == code && (int)row[ccNumber] == number)
                {
                    classRow = row;
                    break;
                }
            }
            return classRow;
        }

        public static IEnumerable<Class> GetScheduleFromStudentID(string id)
        {
            DataRow row = Data.Tables["Student"].Rows.Find(id);
            if (row is null) return null;
            DataTable tStudent = Data.Tables["Student"];
            DataColumn cApplied = tStudent.Columns["Applied"];

            return from tuple in ((string Code, int Grade, int Number)[])row[cApplied]
                   select GetClass(tuple.Code, tuple.Grade, tuple.Number);
        }

        public static string GetNameFromStudentID(string id)
        {
            DataRow row = Data.Tables["Student"].Rows.Find(id);
            if (row is null) return null;
            DataTable tStudent = Data.Tables["Student"];
            DataColumn cName = tStudent.Columns["Name"];
            return row[cName].ToString();
        }

        public static List<Class> ClassDict(string code, int grade) => _classDict[(code, grade)];

        /// <summary>
        ///     Get the number of classes with the lecture code <paramref name="code"/>
        /// </summary>
        public static int GetTheNumberOfClasses(string code, int grade) => ClassDict(code, grade).Count;

        private static void InitializeClassDictionary()
        {
            _classDict.Clear();
            DataTable tLecture = Data.Tables["Lecture"];
            DataColumn cName = tLecture.Columns["Name"];
            DataTable tClass = Data.Tables["Class"];
            DataColumn cCode = tClass.Columns["Code"];
            DataColumn cNumber = tClass.Columns["Number"];
            DataColumn ccGrade = tClass.Columns["Grade"];
            DataColumn cTeacher = tClass.Columns["Teacher"];
            DataColumn cTime = tClass.Columns["Time"];
            DataColumn cNote = tClass.Columns["Note"];
            DataTable tStudent = Data.Tables["Student"];
            DataColumn cApplied = tStudent.Columns["Applied"];
            DataColumn cID = tStudent.Columns["ID"];
            Dictionary<(string Code, int Grade, int Number), List<string>> applyDict = new();
            void AddToApplyDict(string code, int grade, int number, string student)
            {
                if (applyDict.TryGetValue((code, grade, number), out List<string> list))
                    list.Add(student);
                else
                    applyDict[(code, grade, number)] = new List<string>() { student };
            }
            foreach (DataRow student in tStudent.Rows)
            {
                (string Code, int Grade, int Number)[] applied = ((string Code, int Grade, int Number)[])student[cApplied];
                string idNum = student[cID].ToString();
                foreach ((string code, int grade, int number) in applied)
                    AddToApplyDict(code, grade, number, idNum);
            }
            foreach (DataRow row in tClass.Rows)
            {
                string code = (string)row[cCode];
                int grade = (int)row[ccGrade];

                if (!_classDict.ContainsKey((code, grade)))
                {
                    _classDict[(code, grade)] = new List<Class>();
                }

                _classDict[(code, grade)].Add(new Class(
                    Name: tLecture.Rows.Find(new object[] { code, grade })[cName].ToString(),
                    Code: code,
                    Grade: grade,
                    Number: Int32.Parse(row[cNumber].ToString()),
                    Teacher: row[cTeacher].ToString(),
                    Schedule: ((DayOfWeek Day, int Hour)[])row[cTime],
                    Note: row[cNote].ToString(),
                    EnrolledList: new(applyDict[(code, grade, (int)row[cNumber])])
                ));
            }
            foreach (var classList in _classDict.Values)
            {
                classList.Sort((a, b) => a.Number.CompareTo(b.Number));
            }
        }

        private static void InitializeLectureList()
        {
            _lectures.Clear();
            DataTable tLecture = Data.Tables["Lecture"];
            DataColumn cDepartment = tLecture.Columns["Department"];
            DataColumn cName = tLecture.Columns["Name"];
            DataColumn cGrade = tLecture.Columns["Grade"];
            DataColumn cCode = tLecture.Columns["Code"];
            DataColumn cCredit = tLecture.Columns["Credit"];
            foreach (DataRow row in tLecture.Rows)
            {
                string code = row[cCode].ToString();
                int grade = (int)row[cGrade];
                _lectures.Add(new Lecture(
                    Code: code,
                    Department: (Department)Enum.Parse(typeof(Department), (string)row[cDepartment]),
                    Name: (string)row[cName],
                    Grade: grade,
                    NumClass: GetTheNumberOfClasses(code, grade),
                    Credit: (int)row[cCredit]
                ));
            }
        }
    }
}
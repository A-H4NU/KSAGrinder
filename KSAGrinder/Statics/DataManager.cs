using KSAGrinder.Components;

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace KSAGrinder.Statics
{
    public static class DataManager
    {
        public static DataSet Data { get; private set; }

        public static void SetData(DataSet data)
        {
            Data = data;
            InitializeClassDictionary();
        }

        /// <summary>
        /// correspond a code to a list of classes
        /// </summary>
        private static readonly Dictionary<string, List<Class>> _classDict = new Dictionary<string, List<Class>>();

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

        public static Class GetClass(string code, int number) => _classDict[code][number - 1];

        public static IEnumerable<Class> GetScheduleFromStudentID(string id)
        {
            DataRow row = Data.Tables["Student"].Rows.Find(id);
            if (row == null) return null;
            DataTable tStudent = Data.Tables["Student"];
            DataColumn csApplied = tStudent.Columns["Applied"];

            return from tuple in ((string Code, int Number)[])row[csApplied]
                   select _classDict[tuple.Code][tuple.Number - 1];
        }

        public static string GetNameFromStudentID(string id)
        {
            DataRow row = Data.Tables["Student"].Rows.Find(id);
            if (row == null) return null;
            DataTable tStudent = Data.Tables["Student"];
            DataColumn cName = tStudent.Columns["Name"];
            return row[cName].ToString();
        }

        public static List<Class> ClassDict(string lectureCode) => _classDict[lectureCode];

        private static void InitializeClassDictionary()
        {
            _classDict.Clear();
            DataTable tLecture = Data.Tables["Lecture"];
            DataColumn cName = tLecture.Columns["Name"];
            DataTable tClass = Data.Tables["Class"];
            DataColumn cCode = tClass.Columns["Code"];
            DataColumn cNumber = tClass.Columns["Number"];
            DataColumn cTeacher = tClass.Columns["Teacher"];
            DataColumn cTime = tClass.Columns["Time"];
            DataColumn cNote = tClass.Columns["Note"];
            DataTable tStudent = Data.Tables["Student"];
            DataColumn cApplied = tStudent.Columns["Applied"];
            DataColumn cID = tStudent.Columns["ID"];
            var applyDict = new Dictionary<(string Code, int Number), List<string>>();
            void AddToApplyDict(string code, int number, string student)
            {
                if (applyDict.TryGetValue((code, number), out List<string> list))
                    list.Add(student);
                else
                    applyDict[(code, number)] = new List<string>() { student };
            }
            foreach (DataRow student in tStudent.Rows)
            {
                var applied = ((string Code, int Number)[])student[cApplied];
                string idNum = student[cID].ToString();
                foreach ((string code, int number) in applied)
                    AddToApplyDict(code, number, idNum);
            }
            foreach (DataRow row in tClass.Rows)
            {
                string code = (string)row[cCode];

                if (!_classDict.ContainsKey(code))
                {
                    _classDict[code] = new List<Class>();
                }

                _classDict[code].Add(new Class()
                {
                    Code = code,
                    Name = tLecture.Rows.Find(code)[cName].ToString(),
                    Number = Int32.Parse(row[cNumber].ToString()),
                    Teacher = row[cTeacher].ToString(),
                    Schedule = ((DayOfWeek Day, int Hour)[])row[cTime],
                    Note = row[cNote].ToString(),
                    EnrolledList = applyDict[(code, (int)row[cNumber])]
                });
            }
        }
    }
}

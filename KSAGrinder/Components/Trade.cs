using KSAGrinder.Exceptions;
using KSAGrinder.Statics;

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;

namespace KSAGrinder.Components
{
    public class Trade
    {
        private static DataSet _data;

        public string LectureCode { get; private set; }

        public string FromStudent { get; private set; }

        public int FromNumber { get; private set; }

        public string ToStudent { get; private set; }

        public int ToNumber { get; private set; }

        public static void SetData(DataSet data) => _data = data;

        public Trade(string lectureCode, string fromStudent, int fromNumber, string toStudent, int toNumber)
        {
            if (_data == null)
                throw new NoDataException("Data must be provided before any initialization of an instance!");
            LectureCode = lectureCode;
            FromStudent = fromStudent;
            FromNumber = fromNumber;
            ToStudent = toStudent;
            ToNumber = toNumber;
            if (!IsValid)
                throw new ArgumentException("Arguments are not valid.");
        }

        public Trade(string fromStudent, Class fromClass, string toStudent, Class toClass)
        {
            if (_data == null)
                throw new NoDataException("Data must be provided before any initialization of an instance!");
            if (fromClass.Code != toClass.Code)
                throw new ArgumentException("Codes of fromClass and toClass must be same!");
            LectureCode = fromClass.Code;
            FromStudent = fromStudent;
            FromNumber = fromClass.Number;
            ToStudent = toStudent;
            ToNumber = toClass.Number;
            if (!IsValid)
                throw new ArgumentException("Arguments are not valid.");
        }

        public bool IsValid
        {
            get
            {
                if (_data == null)
                    throw new NoDataException("Data must be provided.");
                DataTable tStudent = _data.Tables["Student"];
                DataTable tClass = _data.Tables["Class"];
                if (FromNumber == ToNumber) return false;
                object[] certificates = new object[]
                {
                    tStudent.Rows.Find(FromStudent),
                    tStudent.Rows.Find(ToStudent),
                    DataManager.GetClassRow(LectureCode, FromNumber),
                    DataManager.GetClassRow(LectureCode, ToNumber)
                };
                return certificates.All(o => o != null);
            }
        }

        private static IEnumerable<Class> MoveClass(IEnumerable<Class> schedule, string code, int to)
        {
            var scheduleL = schedule.ToList();
            for (int i = 0; i < scheduleL.Count; ++i)
            {
                if (scheduleL[i].Code == code)
                {
                    scheduleL.RemoveAt(i);
                    scheduleL.Insert(i, DataManager.GetClass(code, to));
                    break;
                }
            }
            return scheduleL;
        }

        public static IEnumerable<IEnumerable<Trade>> GenerateTrades(string studentID, IEnumerable<Class> original, IEnumerable<Class> target, int height)
        {
            if (original.ToHashSet().Intersect(target.ToHashSet()).Count() == original.Count())
                return new IEnumerable<Trade>[] { Enumerable.Empty<Trade>() };
            if (height <= 0) return null;
            var toTrade = new List<(string Code, int From, int To)>();
            foreach (Class cls1 in original)
            {
                foreach (Class cls2 in target)
                {
                    if (cls1.Code == cls2.Code && cls1.Number != cls2.Number)
                    {
                        toTrade.Add((cls1.Code, cls1.Number, cls2.Number));
                        break;
                    }
                }
            }
            IEnumerable<IEnumerable<Trade>> result = Enumerable.Empty<IEnumerable<Trade>>();
            foreach ((string code, int fromN, int toN) in toTrade)
            {
                var affordabilityDictionary = new Dictionary<IEnumerable<Class>, int>();
                int Affordability(IEnumerable<Class> schedule)
                {
                    if (affordabilityDictionary.ContainsKey(schedule))
                        return affordabilityDictionary[schedule];

                    IEnumerator<Class> enumerator = schedule.GetEnumerator();
                    var timetable = new HashSet<(DayOfWeek, int)>();
                    while (enumerator.MoveNext())
                    {
                        Class @class = enumerator.Current;
                        if (@class.Code != code) @class = DataManager.GetClass(code, toN);
                        foreach ((DayOfWeek, int) hour in @class.Schedule)
                        {
                            if (!timetable.Add(hour))
                                return affordabilityDictionary[schedule] = 0;
                        }
                    }
                    return affordabilityDictionary[schedule] = 1;
                }

                List<string> studentsInClass = DataManager.GetClass(code, fromN).EnrolledList;
                Debug.Assert(studentsInClass.Remove(studentID));
                var schedules = (from student in studentsInClass
                                 select (student, MoveClass(DataManager.GetScheduleFromStudentID(student), code, toN))).ToList();
                schedules.Sort((a, b) => Affordability(b.Item2) - Affordability(a.Item2));
                IEnumerable<IEnumerable<Trade>> subresult = Enumerable.Empty<IEnumerable<Trade>>();
                foreach ((string student, IEnumerable<Class> schedule) in schedules)
                {
                    IEnumerable<IEnumerable<Trade>> k
                        = GenerateTrades(
                            student,
                            DataManager.GetScheduleFromStudentID(student),
                            schedule,
                            height - 1);
                    if (k == null) continue;
                    subresult = subresult.Concat(
                        from trades in k
                        select trades.Concat(new Trade[] { new Trade(code, studentID, fromN, student, toN) })
                        );
                }
                result = result.Concat(subresult);
            }
            if (result.Any()) return result;
            return null;
        }
    }
}

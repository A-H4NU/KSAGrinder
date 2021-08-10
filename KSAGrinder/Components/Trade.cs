using KSAGrinder.Exceptions;
using KSAGrinder.Statics;

using System;
using System.Data;

namespace KSAGrinder.Components
{
    [Obsolete]
    public readonly struct Trade : IEquatable<Trade>
    {
        private static DataSet _data;

        public string LectureCode { get; }

        public string StudentA { get; }

        public int NumberA { get; }

        public string StudentB { get; }

        public int NumberB { get; }

        public static void SetData(DataSet data) => _data = data;

        public Trade(string lectureCode, string studentA, int numberA, string studentB, int numberB)
        {
            if (_data == null)
                throw new NoDataException("Data must be provided before any initialization of an instance!");
            LectureCode = lectureCode;
            StudentA = studentA;
            NumberA = numberA;
            StudentB = studentB;
            NumberB = numberB;
            if (!IsValid)
                throw new ArgumentException("Arguments are not valid.");
        }

        public Trade(string studentA, in Class classA, string studentB, in Class classB)
        {
            if (_data == null)
                throw new NoDataException("Data must be provided before any initialization of an instance!");
            if (classA.Code != classB.Code)
                throw new ArgumentException("Codes of fromClass and toClass must be same!");
            LectureCode = classA.Code;
            StudentA = studentA;
            NumberA = classA.Number;
            StudentB = studentB;
            NumberB = classB.Number;
            if (!IsValid)
                throw new ArgumentException("Arguments are not valid.");
        }

        public bool IsValid
        {
            get
            {
                if (_data == null)
                    throw new NoDataException("Data must be provided.");
                if (NumberA == NumberB) return false;
                return DataManager.StudentExists(StudentA)
                        && DataManager.StudentExists(StudentB)
                        && DataManager.ClassExists(LectureCode, NumberA)
                        && DataManager.ClassExists(LectureCode, NumberB);
            }
        }

        public Trade Inverse => new Trade(LectureCode, StudentA, NumberA, StudentB, NumberA);

        //private static IEnumerable<Class> MoveClass(IEnumerable<Class> schedule, string code, int to)
        //{
        //    var scheduleL = schedule.ToList();
        //    for (int i = 0; i < scheduleL.Count; ++i)
        //    {
        //        if (scheduleL[i].Code == code)
        //        {
        //            scheduleL.RemoveAt(i);
        //            scheduleL.Insert(i, DataManager.GetClass(code, to));
        //            break;
        //        }
        //    }
        //    return scheduleL;
        //}


        //public static IEnumerable<IEnumerable<Trade>> GenerateTrade(string studentID, Schedule targetSchedule)
        //{
        //    var reference = new Dictionary<string, Schedule>();
        //    Schedule GetCurrentSchedule(string student)
        //    {
        //        if (reference.ContainsKey(student))
        //            return reference[student];
        //        return new Schedule(DataManager.GetScheduleFromStudentID(student));
        //    }
        //    bool MoveClass(string student, string lecture, int number)
        //    {
        //        Schedule schedule = GetCurrentSchedule(student);
        //        bool result = schedule.MoveClass(lecture, number);
        //        reference[student] = schedule;
        //        return result;
        //    }

        //    void GenerateTradesRecursive(string student, Schedule target)
        //    {

        //    }
        //}

        //public static IEnumerable<IEnumerable<Trade>> GenerateTrades(
        //    IEnumerable<(string StudentID, Schedule Schedule)> targets,
        //    TradeCapture tradeCapture,
        //    int depth,
        //    int maxDepth)
        //{
        //    if (targets.All(tuple => tradeCapture.GetScheduleOf(tuple.StudentID).Equals(tuple.Schedule)))
        //    {
        //        yield return tradeCapture;
        //        yield break;
        //    }

        //    foreach ((string studentID, Schedule targetSchedule) in targets)
        //    {
        //        IEnumerable<string> lecturesToMove = from cls in tradeCapture.GetScheduleOf(studentID)
        //                                             where targetSchedule.GetClassNumber(cls.Code) != cls.Number
        //                                             select cls.Code;
        //        foreach (string lecture in lecturesToMove)
        //        {
        //            IEnumerable<string> studentsToTryTrading = from std in tradeCapture.GetEnrollListOf(lecture, targetSchedule.GetClassNumber(lecture))
        //                                                       where targets.Any(tuple => std != tuple.StudentID)
        //                                                       select std;

        //        }
        //    }
        //}

        public override string ToString() => $"{{ Trade {LectureCode} between {StudentA}, {NumberA} and {StudentB}, {NumberB} }}";

        public bool Equals(Trade other)
        {
            if (LectureCode != other.LectureCode) return false;
            (string, int) thisA = (StudentA, NumberA), thisB = (StudentB, NumberB);
            (string, int) otherA = (other.StudentA, other.NumberA), otherB = (other.StudentB, other.NumberB);
            return (thisA == otherA && thisB == otherB) || (thisA == otherB && thisB == otherA);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace KSAGrinder.Components
{
    public readonly struct Class : IEquatable<Class>
    {
        public Class(
            string name,
            string code,
            int grade,
            int number,
            string teacher,
            (DayOfWeek Day, int Hour)[] schedule,
            List<string> enrolledList,
            string note)
        {
            Name = name;
            Code = code;
            Grade = grade;
            Number = number;
            Teacher = teacher;
            Schedule = schedule;
            EnrolledList = enrolledList;
            Note = note;
        }

        private static readonly Dictionary<DayOfWeek, string> _dayToShortKor = new Dictionary<DayOfWeek, string>()
        {
            {DayOfWeek.Monday, "월"},
            {DayOfWeek.Tuesday, "화"},
            {DayOfWeek.Wednesday, "수"},
            {DayOfWeek.Thursday, "목"},
            {DayOfWeek.Friday, "금"},
            {DayOfWeek.Saturday, "토"},
            {DayOfWeek.Sunday, "일"},
        };

        public string Name { get; }
        public string Code { get; }
        public int Grade { get; }
        public int Number { get; }
        public string Teacher { get; }

        public (DayOfWeek Day, int Hour)[] Schedule { get; }
        public string DayTime
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < Schedule.Length; ++i)
                {
                    if (i != 0) sb.Append(" ");
                    sb.Append(_dayToShortKor[Schedule[i].Day]);
                    sb.Append(Schedule[i].Hour);
                }
                return sb.ToString();
            }
        }
        public int Enroll => EnrolledList.Count;
        public string Note { get; }
        public List<string> EnrolledList { get; }

        public override string ToString() => $"{Code} {Name} {Number}";

        public bool Equals(Class other) => (Code, Grade, Number) == (other.Code, other.Grade, other.Number);
    }
}

using KSAGrinder.Statics;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace KSAGrinder.Components
{
    public readonly record struct Class(
        string Name,
        string Code,
        int Grade,
        int Number,
        string Teacher,
        (DayOfWeek Day, int Hour)[] Schedule,
        ReadOnlyCollection<string> EnrolledList,
        string Note) : IEquatable<Class>
    {
        private static readonly Dictionary<DayOfWeek, string> _dayToShortKor = new()
        {
            {DayOfWeek.Monday, "월"},
            {DayOfWeek.Tuesday, "화"},
            {DayOfWeek.Wednesday, "수"},
            {DayOfWeek.Thursday, "목"},
            {DayOfWeek.Friday, "금"},
            {DayOfWeek.Saturday, "토"},
            {DayOfWeek.Sunday, "일"},
        };

        public int Enroll => EnrolledList.Count;

        public int Credit => DataManager.GetLecture(Code, Grade).Credit;
        public string DayTime
        {
            get
            {
                StringBuilder sb = new();
                for (int i = 0; i < Schedule.Length; ++i)
                {
                    if (i != 0) sb.Append(' ');
                    sb.Append(_dayToShortKor[Schedule[i].Day]);
                    sb.Append(Schedule[i].Hour);
                }
                return sb.ToString();
            }
        }

        public bool Equals(Class other) => (Code, Grade, Number) == (other.Code, other.Grade, other.Number);

        public override int GetHashCode() => (Code, Grade, Number).GetHashCode();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "<Pending>")]
        private bool PrintMembers(StringBuilder sb)
        {
            sb.Append($"Code = {Code}, Name = {Name}, Grade = {Grade}, Number = {Number}");
            return true;
        }
    }
}

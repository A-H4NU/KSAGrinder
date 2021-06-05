using System;
using System.Collections.Generic;
using System.Text;

namespace KSAGrinder.Components
{
    public class Class
    {
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

        public string Name { get; set; }
        public string Code { get; set; }
        public int Number { get; set; }
        public string Teacher { get; set; }

        public (DayOfWeek Day, int Hour)[] Schedule { get; set; }
        public string DayTime
        {
            get
            {
                var sb = new StringBuilder();
                for (int i = 0; i < Schedule.Length; ++i)
                {
                    if (i != 0) sb.Append(" ");
                    sb.Append(_dayToShortKor[Schedule[i].Day]);
                    sb.Append(Schedule[i].Hour);
                }
                return sb.ToString();
            }
        }
        public string Enroll { get; set; }
        public string Note { get; set; }

        public List<string> EnrolledList { get; set; }
    }
}

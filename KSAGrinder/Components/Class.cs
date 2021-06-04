using System;
using System.Collections.Generic;
using System.Text;

namespace KSAGrinder.Components
{
    public struct Class
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public string Number { get; set; }
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
                    sb.Append(Schedule[i].Day.ToString().Substring(0, 3).ToUpper());
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

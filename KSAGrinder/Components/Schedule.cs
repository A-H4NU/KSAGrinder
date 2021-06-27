using System;
using System.Collections;
using System.Collections.Generic;

namespace KSAGrinder.Components
{
    public class Schedule : ICollection<Class>
    {
        private readonly List<Class> _classList = new List<Class>();

        public Schedule() { }

        public Schedule(IEnumerable<Class> classes) => _classList.AddRange(classes);

        public int Count => _classList.Count;

        public bool IsReadOnly => false;

        public void Add(Class item) => _classList.Add(item);
        public void AddRange(IEnumerable<Class> collection) => _classList.AddRange(collection);
        public void Clear() => _classList.Clear();
        public bool Contains(Class item) => _classList.Contains(item);
        public void CopyTo(Class[] array, int arrayIndex) => _classList.CopyTo(array, arrayIndex);
        public IEnumerator<Class> GetEnumerator() => _classList.GetEnumerator();
        public bool Remove(Class item) => _classList.Remove(item);
        IEnumerator IEnumerable.GetEnumerator() => _classList.GetEnumerator();

        /// <summary>
        /// Checks whether the schedule has overlapping classes
        /// </summary>
        public bool IsValid
        {
            get
            {
                var schedule = new HashSet<(DayOfWeek, int)>();
                foreach (Class @class in _classList)
                {
                    foreach ((DayOfWeek, int) hour in @class.Schedule)
                    {
                        if (!schedule.Add(hour))
                            return false;
                    }
                }
                return true;
            }
        }

        public void CopyTo(Schedule schedule)
        {
            schedule._classList.Clear();
            schedule.AddRange(_classList);
        }

        private double EvaluateNEmpty(int n)
        {
            var schedule = new HashSet<(DayOfWeek, int)>();
            foreach (Class @class in _classList)
            {
                foreach ((DayOfWeek, int) hour in @class.Schedule)
                {
                    schedule.Add(hour);
                }
            }
            double score = 100.0;
            for (DayOfWeek day = DayOfWeek.Monday; day <= DayOfWeek.Friday; day++)
            {
                if (schedule.Contains((day, n)))
                {
                    score -= 20.0;
                }
            }
            return Math.Round(score, 2);
        }

        public double Evaluate1Empty => EvaluateNEmpty(1);
        public double Evaluate4Empty => EvaluateNEmpty(4);
        public double Evaluate5Empty => EvaluateNEmpty(5);
        public double EvaluateCompact
        {
            get
            {
                var lastClass = new Dictionary<DayOfWeek, int>();
                for (DayOfWeek day = DayOfWeek.Monday; day <= DayOfWeek.Friday; day++)
                    lastClass[day] = 0;
                foreach (Class @class in _classList)
                {
                    foreach ((DayOfWeek day, int hour) in @class.Schedule)
                    {
                        if (lastClass[day] < hour)
                            lastClass[day] = hour;
                    }
                }
                double score = 0.0;
                foreach (int lastHour in lastClass.Values)
                {
                    score += 100.0 * (14 - lastHour) / (14.0 * 5);
                }
                return Math.Round(score, 2);
            }
        }
    }
}

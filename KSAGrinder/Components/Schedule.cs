using KSAGrinder.Extensions;
using KSAGrinder.Statics;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace KSAGrinder.Components
{
    public class Schedule : ICollection<Class>
    {
        private readonly List<Class> _classList = new List<Class>();

        public static string OriginalScheduleID { get; set; }

        public Schedule() { }

        public Schedule(IEnumerable<Class> classes) => _classList.AddRange(classes);

        public Schedule(IEnumerator<Class> classes)
        {
            while (classes.MoveNext())
            {
                _classList.Add(classes.Current);
            }
        }

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

        public double EvaluateLowNumMoves
        {
            get
            {
                IEnumerable<Class> original = DataManager.GetScheduleFromStudentID(OriginalScheduleID);
                if (original == null) return 0;
                int count = 0;
                foreach (Class cls1 in original)
                {
                    foreach (Class cls2 in _classList)
                    {
                        if (cls1.Code == cls2.Code)
                        {
                            if (cls1.Number != cls2.Number)
                                ++count;
                            break;
                        }
                    }
                }
                return Math.Round((1 - (double)count / original.Count()) * 100, 2);
            }
        }

        public static bool CheckValid(IEnumerable<Class> classes)
        {
            var schedule = new List<(DayOfWeek, int)>();
            foreach (Class @class in classes)
            {
                foreach ((DayOfWeek, int) hour in @class.Schedule)
                {
                    if (schedule.Contains(hour)) return false;
                    schedule.Add(hour);
                }
            }

            return true;
        }

        public static bool CheckValid(IEnumerable<Class> classes, out Schedule result)
        {
            var schedule = new List<(DayOfWeek, int)>();
            result = null;
            var list = new List<Class>();
            foreach (Class @class in classes)
            {
                foreach ((DayOfWeek, int) hour in @class.Schedule)
                {
                    if (schedule.Contains(hour)) return false;
                    schedule.Add(hour);
                }
                list.Add(@class);
            }
            result = new Schedule(list);
            return true;
        }

        public IEnumerable<Schedule> CombinationsOfSchedule(IEnumerable<string> pinnedLectures = null, int maxMove = -1, bool onlyValid = true)
        {
            if (pinnedLectures == null) pinnedLectures = Enumerable.Empty<string>();
            if (maxMove == 0)
            {
                yield return this;
                yield break;
            }

            var codeToIndex = Enumerable.Range(0, _classList.Count).ToDictionary(i => _classList[i].Code);
            string[] notPinnedLectures = _classList.Select(@class => @class.Code).Except(pinnedLectures).ToArray();
            int n_notPinned = notPinnedLectures.Count();
            if (maxMove < 0 || maxMove > n_notPinned) maxMove = n_notPinned;
            int[] currentNumbersNotPinned = notPinnedLectures.Select(code => _classList[codeToIndex[code]].Number).ToArray();

            IEnumerable<Class> GenerateScheduleFromCombination(string[] lecturesToMoveAsCode, int[] combination)
            {
                int index = 0;
                var classesOfSchedule = new Class[_classList.Count];
                for (int i = 0; i < _classList.Count; i++)
                {
                    string lectureCode = _classList[i].Code;
                    yield return lecturesToMoveAsCode.Contains(lectureCode) ? DataManager.GetClass(lectureCode, combination[index++]) : _classList[i];
                }
            }

            foreach (IEnumerable<int> lecturesToMove in Enumerable.Range(0, n_notPinned).GetCombsFromZeroToK(maxMove))
            {
                string[] lecturesToMoveAsCode = lecturesToMove.Select(i => notPinnedLectures[i]).ToArray();
                IEnumerable<IEnumerable<int>> sequences = lecturesToMoveAsCode.Select(lectureCode =>
                {
                    IEnumerable<int> allNumbers = Enumerable.Range(1, DataManager.NumberOfClasses(lectureCode));
                    return allNumbers.Except(new int[] { _classList[codeToIndex[lectureCode]].Number });
                }); // sequences.Count() == lecturesToMove.Count()
                IEnumerable<int[]> combinations = sequences.CartesianProduct().Select(i => i.ToArray()); // dim(combinations)[1] == lecturesToMove.Count()
                foreach (int[] combination in combinations)
                {
                    IEnumerable<Class> schedule = GenerateScheduleFromCombination(lecturesToMoveAsCode, combination);
                    if (onlyValid)
                    {
                        if (CheckValid(schedule, out Schedule result))
                            yield return result;
                    }
                    else
                    {
                        yield return new Schedule(schedule);
                    }
                }
            }
        }
    }
}
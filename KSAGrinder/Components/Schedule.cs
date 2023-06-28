using KSAGrinder.Extensions;
using KSAGrinder.Statics;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace KSAGrinder.Components
{
    public class Schedule : ICollection<Class>, ICloneable
    {
        private readonly List<Class> _classList = new();

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
                HashSet<(DayOfWeek, int)> schedule = new();
                foreach (Class @class in _classList)
                {
                    //if (@class.Schedule.ToHashSet().Count != @class.Schedule.Count())
                    //    Debug.Assert(false);
                    foreach ((DayOfWeek, int) hour in @class.Schedule)
                    {
                        if (!schedule.Add(hour))
                            return false;
                    }
                }
                return true;
            }
        }

        //public object OverlappingClasses
        //{
        //    get
        //    {
        //        var schedule = new HashSet<(DayOfWeek, int)>();
        //        foreach (Class @class in _classList)
        //        {
        //            foreach ((DayOfWeek, int) hour in @class.Schedule)
        //            {
        //                if (schedule.Add(hour)) continue;
        //                foreach (Class class2 in _classList)
        //                {
        //                    if (!class2.Equals(@class) && class2.Schedule.Contains(hour))
        //                        return (@class, class2);
        //                }
        //            }
        //        }
        //        return null;
        //    }
        //}

        public void CopyTo(Schedule schedule)
        {
            schedule._classList.Clear();
            schedule.AddRange(_classList);
        }

        public bool MoveClass(string code, int grade, int number)
        {
            int index = _classList.FindIndex(cls => cls.Code == code && cls.Grade == grade);
            if (index == -1) return false;
            _classList[index] = DataManager.GetClass(code, grade, number);
            return true;
        }

        /// <summary>
        /// Returns the number of a lecture
        /// </summary>
        /// <param name="code">A lecture code to find its number</param>
        /// <returns>The class number; -1 when it is not found</returns>
        public int GetClassNumber(string code, int grade)
        {
            foreach (Class @class in _classList)
            {
                if (@class.Code == code && @class.Grade == grade)
                    return @class.Number;
            }
            return -1;
        }

        private double EvaluateNEmpty(int n)
        {
            HashSet<(DayOfWeek, int)> schedule = new();
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
                Dictionary<DayOfWeek, int> lastClass = new();
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
                        if ((cls1.Code, cls1.Grade) == (cls2.Code, cls2.Grade))
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
            List<(DayOfWeek, int)> schedule = new();
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
            List<(DayOfWeek, int)> schedule = new();
            result = null;
            List<Class> list = new();
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

        public IEnumerable<Schedule> Combination(IEnumerable<(string, int)> pinnedLectures = null, int maxMove = -1, bool onlyValid = true)
        {
            if (pinnedLectures == null) pinnedLectures = Enumerable.Empty<(string, int)>();
            if (maxMove == 0)
            {
                yield return this;
                yield break;
            }

            Dictionary<(string, int), int> lectureToIndex = Enumerable.Range(0, _classList.Count)
                                                                      .ToDictionary(i => (_classList[i].Code, _classList[i].Grade));
            (string, int)[] notPinnedLectures = _classList.Select(@class => (@class.Code, @class.Grade)).Except(pinnedLectures).ToArray();
            int n_notPinned = notPinnedLectures.Length;
            if (maxMove < 0 || maxMove > n_notPinned) maxMove = n_notPinned;
            int[] currentNumbersNotPinned = notPinnedLectures.Select(tuple => _classList[lectureToIndex[(tuple.Item1, tuple.Item2)]].Number).ToArray();

            IEnumerable<Class> GenerateScheduleFromCombination((string, int)[] lecturesToMove, int[] combination)
            {
                int index = 0;
                Class[] classesOfSchedule = new Class[_classList.Count];
                for (int i = 0; i < _classList.Count; i++)
                {
                    string code = _classList[i].Code;
                    int grade = _classList[i].Grade;
                    yield return lecturesToMove.Contains((code, grade)) ? DataManager.GetClass(code, grade, combination[index++]) : _classList[i];
                }
            }

            foreach (IEnumerable<int> lecturesToMove in Enumerable.Range(0, n_notPinned).GetCombsFromZeroToK(maxMove))
            {
                (string, int)[] lecturesToMoveAsCodeGrade = lecturesToMove.Select(i => notPinnedLectures[i]).ToArray();
                IEnumerable<IEnumerable<int>> sequences = lecturesToMoveAsCodeGrade.Select(tuple =>
                {
                    IEnumerable<int> allNumbers = Enumerable.Range(1, DataManager.GetTheNumberOfClasses(tuple.Item1, tuple.Item2));
                    return allNumbers.Except(new int[] { _classList[lectureToIndex[tuple]].Number });
                }); // sequences.Count() == lecturesToMove.Count()
                IEnumerable<int[]> combinations = sequences.CartesianProduct().Select(i => i.ToArray()); // dim(combinations)[1] == lecturesToMove.Count()
                foreach (int[] combination in combinations)
                {
                    IEnumerable<Class> schedule = GenerateScheduleFromCombination(lecturesToMoveAsCodeGrade, combination);
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

        public static IEnumerable<ClassMove> Difference(string studentId, Schedule from, Schedule to)
        {
            List<ClassMove> res = new();
            List<Class> tolist = to.ToList();
            foreach (Class clsFrom in from)
            {
                int idx = tolist.FindIndex(cls => cls.Code == clsFrom.Code && cls.Grade == clsFrom.Grade && cls.Number != clsFrom.Number);
                if (idx != -1)
                    res.Add(new ClassMove(studentId, clsFrom.Code, clsFrom.Grade, clsFrom.Number, tolist[idx].Number));
            }
            return res;
        }

        public override bool Equals(object obj)
        {
            if (obj is IEnumerable<Class> other)
            {
                return this.ToHashSet().SetEquals(other.ToHashSet());
            }
            return false;
        }

        public override int GetHashCode() => _classList.Aggregate(0, (val, cls) => val ^ cls.GetHashCode());

        public object Clone() => new Schedule(this);
    }
}
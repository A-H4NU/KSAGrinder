using KSAGrinder.Extensions;
using KSAGrinder.Statics;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using _Schedule = System.Collections.Generic.IEnumerable<KSAGrinder.Components.Class>;

namespace KSAGrinder.Components
{
    public readonly struct Schedule : IReadOnlyCollection<Class>, ICloneable, IEquatable<Schedule>
    {
        private readonly Class[] _classList;

        public static string OriginalScheduleID { get; set; }

        public Schedule() : this(Enumerable.Empty<Class>()) { }

        public Schedule(_Schedule classes)
        {
            _classList = classes.ToArray();
            IsValid = CheckValid(classes);
        }

        public int Count => _classList.Length;

        public bool Contains(Class item) => _classList.Contains(item);
        public IEnumerator<Class> GetEnumerator() => _classList.AsEnumerable().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _classList.GetEnumerator();

        /// <summary>
        /// Checks whether the schedule has overlapping classes on the same day/time
        /// </summary>
        public bool IsValid { get; init; }

        /// <summary>
        /// Returns a new instance that is obtained by moving a class to another.
        /// </summary>
        public Schedule MovedClass(string code, int grade, int number)
        {
            return MovedClass(this, code, grade, number);
        }

        /// <summary>
        /// Static version of <see cref="MovedClass(String, Int32, Int32)"/>.
        /// </summary>
        public static Schedule MovedClass(_Schedule schedule, string code, int grade, int number)
        {
            return new(schedule.Select(@class =>
            {
                if (@class.Code == code && @class.Grade == grade)
                    return DataManager.GetClass(code, grade, number);
                return @class;
            }));
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
                _Schedule original = DataManager.GetScheduleFromStudentID(OriginalScheduleID);
                if (original is null) return 0;
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

        public static bool CheckValid(_Schedule classes)
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

        public static bool CheckValid(_Schedule classes, out Schedule result)
        {
            List<(DayOfWeek, int)> schedule = new();
            result = default;
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
            Class[] classList = _classList;

            pinnedLectures ??= Enumerable.Empty<(string, int)>();
            if (maxMove == 0)
            {
                yield return this;
                yield break;
            }

            Dictionary<(string, int), int> lectureToIndex = Enumerable.Range(0, classList.Length)
                                                                      .ToDictionary(i => (classList[i].Code, classList[i].Grade));
            (string Code, int Grade)[] notPinnedLectures = (from @class in _classList
                                                            select (@class.Code, @class.Grade))
                                                           .Except(pinnedLectures)
                                                           .ToArray();
            int n_notPinned = notPinnedLectures.Length;
            if (maxMove < 0 || maxMove > n_notPinned) maxMove = n_notPinned;
            int[] currentNumbersNotPinned = (from tuple in notPinnedLectures
                                             select classList[lectureToIndex[(tuple.Code, tuple.Grade)]].Number)
                                            .ToArray();

            _Schedule GenerateScheduleFromCombination((string, int)[] lecturesToMove, int[] combination)
            {
                int index = 0;
                Class[] classesOfSchedule = new Class[classList.Length];
                for (int i = 0; i < classList.Length; i++)
                {
                    string code = classList[i].Code;
                    int grade = classList[i].Grade;
                    yield return lecturesToMove.Contains((code, grade)) ? DataManager.GetClass(code, grade, combination[index++]) : classList[i];
                }
            }

            static IEnumerable<int> RangeExceptOne(int start, int count, int except)
            {
                int end = start + count;
                for (int i = start; i < end; ++i)
                {
                    if (i != except)
                        yield return i;
                }
            }

            foreach (IEnumerable<int> lectureIndicesToMove in Enumerable.Range(0, n_notPinned).GetCombsFromZeroToK(maxMove))
            {
                (string Code, int Grade)[] lecturesToMove = (from i in lectureIndicesToMove
                                                             select notPinnedLectures[i])
                                                            .ToArray();
                IEnumerable<IEnumerable<int>> sequences = lecturesToMove.Select(tuple =>
                {
                    int n_classes = DataManager.GetTheNumberOfClasses(tuple.Code, tuple.Grade);
                    int cur_classNum = classList[lectureToIndex[tuple]].Number;
                    return RangeExceptOne(1, n_classes, cur_classNum);
                }); // sequences.Count() == lecturesToMove.Count()
                IEnumerable<int[]> combinations = sequences.CartesianProduct().Select(i => i.ToArray()); // dim(combinations)[1] == lecturesToMove.Count()
                foreach (int[] combination in combinations)
                {
                    _Schedule schedule = GenerateScheduleFromCombination(lecturesToMove, combination);
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
            static bool OnlyDifferNumber(Class class1, Class class2)
                => class1.Code == class2.Code && class1.Grade == class2.Grade && class1.Number != class2.Number;

            List<ClassMove> res = new();
            List<Class> tolist = to.ToList();
            foreach (Class clsFrom in from)
            {
                int idx = tolist.FindIndex(cls => OnlyDifferNumber(cls, clsFrom));
                if (idx != -1)
                    res.Add(new ClassMove(studentId, clsFrom.Code, clsFrom.Grade, clsFrom.Number, tolist[idx].Number));
            }
            return res;
        }

        public override bool Equals(object obj)
        {
            if (obj is _Schedule other)
            {
                return this.ToHashSet().SetEquals(other);
            }
            return false;
        }

        public override int GetHashCode() => _classList.Aggregate(0, (val, cls) => val ^ cls.GetHashCode());

        public object Clone() => new Schedule(this);

        public bool Equals(Schedule other)
        {
            if (Count != other.Count)
                return false;
            return this.ToHashSet().SetEquals(other);
        }

        public static bool operator ==(Schedule left, Schedule right) => left.Equals(right);

        public static bool operator !=(Schedule left, Schedule right) => !(left == right);
    }
}
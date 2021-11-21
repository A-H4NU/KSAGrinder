using KSAGrinder.Exceptions;
using KSAGrinder.Extensions;
using KSAGrinder.Statics;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace KSAGrinder.Components
{
    public class TradeCapture : ICollection<ClassMove>
    {
        private readonly List<ClassMove> _classMoves;
        private readonly Dictionary<string, Schedule> _capturedSchedules;
        private readonly Dictionary<(string, int), List<string>> _capturedClassEnrollLists;

        public TradeCapture()
        {
            _classMoves = new List<ClassMove>();
            _capturedSchedules = new Dictionary<string, Schedule>();
            _capturedClassEnrollLists = new Dictionary<(string, int), List<string>>();
        }

        public TradeCapture(IEnumerable<ClassMove> collection) : this() => AddRange(collection);

        public IReadOnlyCollection<Class> GetScheduleOf(string studentId)
            => PrivateGetScheduleOf(studentId, false).ToList().AsReadOnly();

        public IReadOnlyCollection<string> GetEnrollListOf(string lectureCode, int number)
            => PrivateGetEnrollListOf(lectureCode, number, false).AsReadOnly();

        /// <summary>
        ///     List of lecture codes that <paramref name="studentId"/> has a move on.
        /// </summary>
        public IEnumerable<string> InvolvedLecturesOf(string studentId)
        {
            var result = new List<string>();
            foreach (ClassMove move in _classMoves)
            {
                if (move.StudentId == studentId && !result.Contains(move.LectureCode))
                    result.Add(move.LectureCode);
            }
            return result;
        }

        /// <summary>
        ///     Check if <paramref name="studentId"/> is involved in the moves.
        /// </summary>
        public bool IsStudentInvoled(string studentId)
        {
            foreach (ClassMove move in _classMoves)
                if (move.StudentId == studentId) return true;
            return false;
        }

        /// <summary>
        ///     The list of strudents involed in the moves.
        /// </summary>
        public IEnumerable<string> StudentsInvolved()
        {
            var list = new List<string>();
            foreach (ClassMove move in _classMoves)
            {
                if (!list.Contains(move.StudentId))
                    list.Add(move.StudentId);
            }
            return list;
        }

        /// <summary>
        ///     Regardless of whether the resulting schedules are valid,
        ///     check that the set of moves can be partitioned as sets of trades. (including all k-trades)
        /// </summary>
        public bool DoesFormTrade() => _classMoves.GroupBy(move => move.LectureCode).All(collection => ClassMove.IsSetOfCycles(collection));

        /// <summary>
        ///     Get the list of tails.
        /// </summary>
        /// <returns>
        ///     The list of <see cref="ClassMove"/> objects
        ///     without any other <see cref="ClassMove"/> objects to continue forward.
        /// </returns>
        /// <example>
        ///     <code>
        ///         var A = new ClassMove("Math", "Alpha", 1, 2);
        ///         var B = new ClassMove("Math", "Beta", 2, 3);
        ///         new TradeCapture(new ClassMove[] {A, B}).TailsOfNoncycles() == [ B ]
        ///     </code>
        /// </example>
        public IEnumerable<ClassMove> TailsOfNoncycles()
        {
            foreach (IGrouping<string, ClassMove> movesOfALecture in _classMoves.GroupBy(move => move.LectureCode))
            {
                if (ClassMove.IsSetOfCycles(movesOfALecture)) continue;

                var leftMoves = new List<ClassMove>(movesOfALecture);
                while (leftMoves.Count > 0)
                {
                    ClassMove root = leftMoves[leftMoves.Count - 1];
                    leftMoves.RemoveAt(leftMoves.Count - 1);
                    ClassMove current;

                    // Continue forward
                    current = root;
                    while (true)
                    {
                        int index = leftMoves.FindIndex(move => current.NumberTo == move.NumberFrom);
                        if (index == -1)
                        {
                            yield return current;
                            break;
                        }
                        current = leftMoves[index];
                        leftMoves.RemoveAt(index);
                    }

                    // Backward
                    current = root;
                    while (true)
                    {
                        int index = leftMoves.FindIndex(move => current.NumberFrom == move.NumberTo);
                        if (index == -1)
                            break;
                        current = leftMoves[index];
                        leftMoves.RemoveAt(index);
                    }
                }
            }
        }

        public bool AreAllSchedulesValid() => _capturedSchedules.Values.All(scd => scd.IsValid);

        public int Count => _classMoves.Count;

        public bool IsReadOnly => false;

        /// <summary>
        /// Adds a trade and apply it.
        /// </summary>
        /// <exception cref="TradeInvalidException"/>
        public void Add(ClassMove move)
        {
            ApplyMoves(move);
            _classMoves.Add(move);
        }

        /// <exception cref="TradeInvalidException"/>
        public void AddRange(IEnumerable<ClassMove> collection)
        {
            foreach (ClassMove element in collection)
                Add(element);
        }

        public void Clear()
        {
            _classMoves.Clear();
            _capturedSchedules.Clear();
            _capturedClassEnrollLists.Clear();
        }

        public bool Contains(ClassMove item) => _classMoves.Contains(item);

        public void CopyTo(ClassMove[] array, int arrayIndex) => _classMoves.CopyTo(array, arrayIndex);

        public IEnumerator<ClassMove> GetEnumerator() => _classMoves.GetEnumerator();

        public int IndexOf(ClassMove item) => _classMoves.IndexOf(item);

        /// <returns>the undone move</returns>
        /// <exception cref="InvalidOperationException"/>
        public ClassMove Pop()
        {
            if (_classMoves.Count == 0)
                throw new InvalidOperationException("The list is empty.");

            ClassMove last = _classMoves[_classMoves.Count-1];
            _classMoves.RemoveAt(_classMoves.Count-1);

            PrivateGetScheduleOf(last.StudentId, false).MoveClass(last.LectureCode, last.NumberFrom);
            PrivateGetEnrollListOf(last.LectureCode, last.NumberTo, false).Remove(last.StudentId);
            PrivateGetEnrollListOf(last.LectureCode, last.NumberFrom, false).Add(last.StudentId);

            return last;
        }

        /// <returns>the list of undone moves</returns>
        /// <exception cref="InvalidOperationException"/>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public IEnumerable<ClassMove> Pop(int n)
        {
            if (n < 0)
                throw new ArgumentOutOfRangeException("n", "n must be nonnegative");
            var list = new List<ClassMove>();
            for (int i = 0; i < n; ++i)
            {
                list.Add(Pop());
            }
            return list;
        }

        public bool Remove(ClassMove item) => throw new NotSupportedException();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private Schedule PrivateGetScheduleOf(string studentId, bool capture = true)
        {
            if (_capturedSchedules.TryGetValue(studentId, out Schedule value))
                return value;
            var res = new Schedule(DataManager.GetScheduleFromStudentID(studentId));
            if (capture) _capturedSchedules[studentId] = res;
            return res;
        }

        private List<string> PrivateGetEnrollListOf(string lectureCode, int number, bool capture = true)
        {
            if (_capturedClassEnrollLists.TryGetValue((lectureCode, number), out List<string> value))
                return value;
            var res = DataManager.GetClass(lectureCode, number).EnrolledList.Clone().ToList();
            if (capture) _capturedClassEnrollLists[(lectureCode, number)] = res;
            return res;
        }

        /// <exception cref="TradeInvalidException"/>
        private void ApplyMoves(ClassMove move)
        {
            // Check that the trade is appliable                                        
            if (!move.IsValid)
                throw new TradeInvalidException(move, false);
            if (!PrivateGetEnrollListOf(move.LectureCode, move.NumberFrom).Contains(move.StudentId))
                throw new TradeInvalidException(move, true);

            // Apply the trade
            PrivateGetScheduleOf(move.StudentId).MoveClass(move.LectureCode, move.NumberTo);
            PrivateGetEnrollListOf(move.LectureCode, move.NumberFrom).Remove(move.StudentId);
            PrivateGetEnrollListOf(move.LectureCode, move.NumberTo).Add(move.StudentId);
        }
    }
}
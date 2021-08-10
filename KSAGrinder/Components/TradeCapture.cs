using KSAGrinder.Exceptions;
using KSAGrinder.Extensions;
using KSAGrinder.Statics;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace KSAGrinder.Components
{
    public class TradeCapture : IList<ClassMove>
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

        public IReadOnlyCollection<Class> GetScheduleOf(string studentID)
            => PrivateGetScheduleOf(studentID, false).ToList().AsReadOnly();

        public IReadOnlyCollection<string> GetEnrollListOf(string lectureCode, int number)
            => PrivateGetEnrollListOf(lectureCode, number, false).AsReadOnly();

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

        public ClassMove this[int index] { get => _classMoves[index]; set => throw new NotSupportedException(); }

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

        public void Clear() => throw new NotSupportedException("Removing an element is not supported.");

        public bool Contains(ClassMove item) => _classMoves.Contains(item);

        public void CopyTo(ClassMove[] array, int arrayIndex) => _classMoves.CopyTo(array, arrayIndex);

        public IEnumerator<ClassMove> GetEnumerator() => _classMoves.GetEnumerator();

        public int IndexOf(ClassMove item) => _classMoves.IndexOf(item);

        public void Insert(int index, ClassMove item)
        {
            if (index == _classMoves.Count)
                Add(item);
            throw new NotSupportedException("Inserting an element in the midst of the list is not supported.");
        }

        public bool Remove(ClassMove item) => throw new NotSupportedException("Removing an element is not supported.");

        public void RemoveAt(int index) => throw new NotSupportedException("Removing an element is not supported.");

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private Schedule PrivateGetScheduleOf(string studentID, bool capture = true)
        {
            if (_capturedSchedules.TryGetValue(studentID, out Schedule value))
                return value;
            var res = new Schedule(DataManager.GetScheduleFromStudentID(studentID));
            if (capture) _capturedSchedules[studentID] = res;
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

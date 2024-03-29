﻿using KSAGrinder.Exceptions;
using KSAGrinder.Extensions;
using KSAGrinder.Statics;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace KSAGrinder.Components
{
    public sealed class TradeCapture : ICollection<ClassMove>
    {
        private readonly List<ClassMove> _classMoves;
        private readonly Dictionary<string, Schedule> _capturedSchedules;
        private readonly Dictionary<(string, int, int), List<string>> _capturedClassEnrollLists;

        public TradeCapture()
        {
            _classMoves = new List<ClassMove>();
            _capturedSchedules = new Dictionary<string, Schedule>();
            _capturedClassEnrollLists = new Dictionary<(string, int, int), List<string>>();
        }

        public TradeCapture(IEnumerable<ClassMove> collection) : this() => AddRange(collection);

        /// <summary>
        /// Copies from <paramref name="copyFrom"/>.
        /// </summary>
        /// <param name="copyFrom"></param>
        private TradeCapture(TradeCapture copyFrom)
        {
            _classMoves = new(copyFrom._classMoves);
            _capturedSchedules = new(copyFrom._capturedSchedules);
            _capturedClassEnrollLists = copyFrom._capturedClassEnrollLists.Keys.ToDictionary(
                key => key,
                key => new List<string>(copyFrom._capturedClassEnrollLists[key]));
        }

        public string Summary
            => $"{InvolvedStudents().Count()}인 참여 / {_classMoves.Count}번 분반 이동{Environment.NewLine}"
               + "참여 학생: " + String.Join(", ", InvolvedStudents().Select(id => id + " " + DataManager.GetNameFromStudentID(id)));

        public int NumberOfStudentsInvolved => InvolvedStudents().Count();

        public string InvolvedStudentsString => String.Join(", ", InvolvedStudents());

        public IEnumerable<(string, IReadOnlyCollection<Class>)> GetCapturedSchedules()
        {
            List<(string, IReadOnlyCollection<Class>)> res = new(_capturedSchedules.Count);
            foreach (KeyValuePair<string, Schedule> pair in _capturedSchedules)
                res.Add((pair.Key, pair.Value.ToList().AsReadOnly()));
            return res;
        }

        public Schedule GetScheduleOf(string studentId)
            => PrivateGetScheduleOf(studentId, false);

        public IReadOnlyCollection<string> GetEnrollListOf(string lectureCode, int grade, int number)
            => PrivateGetEnrollListOf(lectureCode, grade, number, false).AsReadOnly();

        /// <summary>
        ///     List of lecture codes that <paramref name="studentId"/> has a move on.
        /// </summary>
        public IEnumerable<(string, int)> InvolvedLecturesOf(string studentId)
        {
            List<(string, int)> result = new();
            foreach (ClassMove move in _classMoves)
            {
                if (move.StudentId == studentId && !result.Contains((move.Code, move.Grade)))
                    result.Add((move.Code, move.Grade));
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
        public IEnumerable<string> InvolvedStudents()
        {
            List<string> list = new();
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
        public bool DoesFormTrade() => _classMoves.GroupBy(move => move.Code).All(ClassMove.IsSetOfCycles);

        /// <summary>
        ///     Get the list of head-tail tuples of maximal simple paths.
        /// </summary>
        /// <returns>
        ///     The list of (<see cref="ClassMove"/> head, <see cref="ClassMove"/> tail) tuples of maximal simple paths.
        /// </returns>
        /// <example>
        ///     <code>
        ///         var A = new ClassMove("Math", "Alpha", 1, 2);
        ///         var B = new ClassMove("Math", "Beta", 2, 3);
        ///         new TradeCapture(new ClassMove[] {A, B}).TailsOfNoncycles() == [ (A, B) ]
        ///     </code>
        /// </example>
        public IEnumerable<(ClassMove Head, ClassMove Tail)> HeadTailTuplesOfNoncycles()
        {
            foreach (IGrouping<(string Code, int Grade), ClassMove> movesOfALecture in _classMoves.GroupBy(move => (move.Code, move.Grade)))
            {
                if (ClassMove.IsSetOfCycles(movesOfALecture)) continue;

                List<ClassMove> leftMoves = new(movesOfALecture);
                while (leftMoves.Count > 0)
                {
                    ClassMove root = leftMoves[^1];
                    leftMoves.RemoveAt(leftMoves.Count - 1);
                    ClassMove current, tail;

                    // Continue forward
                    current = root;
                    while (true)
                    {
                        int index = leftMoves.FindIndex(move => current.NumberTo == move.NumberFrom);
                        if (index == -1)
                        {
                            tail = current;
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
                        {
                            yield return (current, tail);
                            break;
                        }
                        current = leftMoves[index];
                        leftMoves.RemoveAt(index);
                    }
                }
            }
        }

        public IEnumerable<ClassMove> TailsOfNonCycles()
        {
            foreach (IGrouping<string, ClassMove> movesOfALecture in _classMoves.GroupBy(move => move.Code))
            {
                if (ClassMove.IsSetOfCycles(movesOfALecture)) continue;

                List<ClassMove> leftMoves = new(movesOfALecture);
                while (leftMoves.Count > 0)
                {
                    ClassMove root = leftMoves[^1];
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
                        {
                            break;
                        }
                        current = leftMoves[index];
                        leftMoves.RemoveAt(index);
                    }
                }
            }
        }

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
        //public ClassMove Pop()
        //{
        //    if (_classMoves.Count == 0)
        //        throw new InvalidOperationException("The list is empty.");

        //    ClassMove last = _classMoves[^1];
        //    _classMoves.RemoveAt(_classMoves.Count - 1);

        //    PrivateGetScheduleOf(last.StudentId, false).MoveClass(last.Code, last.Grade, last.NumberFrom);
        //    PrivateGetEnrollListOf(last.Code, last.Grade, last.NumberTo, false).Remove(last.StudentId);
        //    PrivateGetEnrollListOf(last.Code, last.Grade, last.NumberFrom, false).Add(last.StudentId);

        //    return last;
        //}

        /// <returns>the list of undone moves</returns>
        /// <exception cref="InvalidOperationException"/>
        /// <exception cref="ArgumentOutOfRangeException"/>
        //public IEnumerable<ClassMove> Pop(int n)
        //{
        //    if (n < 0)
        //        throw new ArgumentOutOfRangeException(nameof(n), "n must be nonnegative");
        //    List<ClassMove> list = new();
        //    for (int i = 0; i < n; ++i)
        //    {
        //        list.Add(Pop());
        //    }
        //    return list;
        //}

        public TradeCapture Clone() => new(this);

        public bool Remove(ClassMove item) => throw new NotSupportedException();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private Schedule PrivateGetScheduleOf(string studentId, bool capture = true)
        {
            if (_capturedSchedules.TryGetValue(studentId, out Schedule value))
                return value;
            Schedule res = new(DataManager.GetScheduleFromStudentID(studentId));
            if (capture) _capturedSchedules[studentId] = res;
            return res;
        }

        private List<string> PrivateGetEnrollListOf(string lectureCode, int grade, int number, bool capture = true)
        {
            if (_capturedClassEnrollLists.TryGetValue((lectureCode, grade, number), out List<string>? value))
                return value;
            List<string> res = DataManager.GetClass(lectureCode, grade, number).EnrolledList.Clone().ToList();
            if (capture) _capturedClassEnrollLists[(lectureCode, grade, number)] = res;
            return res;
        }

        /// <exception cref="TradeInvalidException"/>
        private void ApplyMoves(ClassMove move)
        {
            // Check that the trade is appliable                                        
            if (!move.IsValid)
                throw new TradeInvalidException(move, false);
            if (!PrivateGetEnrollListOf(move.Code, move.Grade, move.NumberFrom).Contains(move.StudentId))
                throw new TradeInvalidException(move, true);

            // Apply the trade
            _capturedSchedules[move.StudentId] = PrivateGetScheduleOf(move.StudentId, capture: false)
                                                 .MovedClass(move.Code, move.Grade, move.NumberTo);
            PrivateGetEnrollListOf(move.Code, move.Grade, move.NumberFrom).Remove(move.StudentId);
            PrivateGetEnrollListOf(move.Code, move.Grade, move.NumberTo).Add(move.StudentId);
        }
    }
}
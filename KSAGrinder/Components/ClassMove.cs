using KSAGrinder.Exceptions;
using KSAGrinder.Extensions;
using KSAGrinder.Statics;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KSAGrinder.Components
{
    public readonly struct ClassMove
    {
        public const int MaxLectureMoves = 1;

        public readonly string StudentId;

        public readonly string LectureCode;

        public readonly int NumberFrom, NumberTo;

        public ClassMove(string studentId, string lectureCode, int numberFrom, int numberTo)
        {
            StudentId = studentId;
            LectureCode = lectureCode;
            NumberFrom = numberFrom;
            NumberTo = numberTo;
        }

        public bool IsValid => DataManager.ClassExists(LectureCode, NumberFrom) && DataManager.ClassExists(LectureCode, NumberTo);

        public override string ToString() => $"ClassMove {StudentId}, {LectureCode} {DataManager.NameOfLectureFromCode(LectureCode)} from {NumberFrom} to {NumberTo}";

        public static int Call = 0;
        public static async IAsyncEnumerable<IEnumerable<ClassMove>> GenerateClassMoves(string studentId, Schedule targetSchedule, int maxDepth)
        {
            ++Call;
            var tradeCapture = new TradeCapture();
            var originalSchedule = DataManager.GetScheduleFromStudentID(studentId).ToList();
            foreach (Class targetClass in targetSchedule)
            {
                int index = originalSchedule.FindIndex(cls => cls.Code == targetClass.Code);
                if (index < 0 || originalSchedule[index].Number == targetClass.Number) continue;
                tradeCapture.Add(new ClassMove(studentId, targetClass.Code, originalSchedule[index].Number, targetClass.Number));
            }
            await foreach (var moves in GenerateClassMoves(new[] { (studentId, targetSchedule) }, tradeCapture, 0, maxDepth))
            {
                yield return moves;
            }
        }

        public static async IAsyncEnumerable<IEnumerable<ClassMove>> GenerateClassMoves(
            IEnumerable<(string StudentId, Schedule Schedule)> targets,
            TradeCapture tradeCapture,
            int depth,
            int maxDepth,
            int batchSize = 1024,
            int numThreads = 4)
        {
            if (tradeCapture.DoesFormTrade() &&
                targets.All(tuple => tuple.Schedule.Equals(tradeCapture.GetScheduleOf(tuple.StudentId))) &&
                tradeCapture.AreAllSchedulesValid())
            {
                yield return tradeCapture;
                yield break;
            }

            if (depth >= maxDepth) yield break;

            var sequences = new List<List<(ClassMove, Schedule)>>();
            int card = 1;
            foreach(ClassMove tailMove in tradeCapture.TailsOfNoncycles())
            {
                // have to make a new class move from tailMove.NumberTo
                int numberOfClasses = DataManager.NumberOfClasses(tailMove.LectureCode);
                // Class numbers already targeted (move.ToNumber of some move of tailMove.LectureCode)
                IEnumerable<int> numbersInvolved = from move in tradeCapture where move.LectureCode == tailMove.LectureCode select move.NumberTo;
                IEnumerable<string> studentsInvolved = tradeCapture.StudentsInvolved();
                var currentList = new List<(ClassMove, Schedule)>();

                // For each class numbers of tailMove.LectureCode but not involved
                foreach (int numberTo in Enumerable.Range(1, numberOfClasses).Except(numbersInvolved))
                {
                    // For each students in (tailMove.LectureCode, tailMove.NumberTo) but not in targets
                    foreach (string studentId in tradeCapture.GetEnrollListOf(tailMove.LectureCode, tailMove.NumberTo).Except(studentsInvolved))
                    {
                        var schedule = new Schedule(tradeCapture.GetScheduleOf(studentId));
                        schedule.MoveClass(tailMove.LectureCode, numberTo);
                        IEnumerable<Schedule> options = schedule.Combination(tradeCapture.InvolvedLecturesOf(studentId).Append(tailMove.LectureCode), MaxLectureMoves, false);
                        foreach (Schedule option in options)
                        {
                            (ClassMove, Schedule) toAdd = (new ClassMove(studentId, tailMove.LectureCode, tailMove.NumberTo, numberTo), option);
                            if (option.IsValid) currentList.Insert(0, toAdd);
                            else currentList.Add(toAdd);
                        }
                    }
                }
                if (currentList.Count == 0) yield break;
                sequences.Add(currentList);
                card *= currentList.Count;
            }

            async Task<IEnumerable<IEnumerable<ClassMove>>> ProcessBatch(IEnumerable<IEnumerable<(ClassMove, Schedule)>> batch)
            {
                var localTradeCapture = tradeCapture.Clone();
                var result = new List<IEnumerable<ClassMove>>();
                foreach (var optionsToTry in batch)
                {
                    int n = 0;
                    var targetsToAdd = new List<(string, Schedule)>();
                    foreach ((ClassMove move, Schedule option) in optionsToTry)
                    {
                        try
                        {
                            localTradeCapture.Add(move);
                        }
                        catch (TradeInvalidException)
                        {
                            Debug.WriteLine("Bad move.");
                            n = -1; break;
                        }
                        targetsToAdd.Add((move.StudentId, option));
                        ++n;
                    }
                    if (n == -1) continue;
                    //Debug.Assert(!targetsToAdd.Intersect(targets).Any());
                    await foreach (IEnumerable<ClassMove> moves in GenerateClassMoves(targets.Concat(targetsToAdd), localTradeCapture, depth + 1, maxDepth))
                    {
                        result.Add(moves);
                    }
                    tradeCapture.Pop(n);
                }
                return result;
            }

            numThreads = (int)Math.Min(Math.Ceiling((double)card / batchSize), numThreads);
            var tasks = new Task<IEnumerable<IEnumerable<ClassMove>>>[numThreads];
            foreach (var batch in sequences.CartesianProduct().Batch(batchSize))
            {
                if (tasks.All(task => task != null))
                {
                    int index = Task.WaitAny(tasks);
                    foreach (var moves in await tasks[index])
                        yield return moves;
                    tasks[index] = ProcessBatch(batch);
                    tasks[index].Start();
                }
                else
                {
                    int index = 0;
                    while (tasks[index] != null) ++index;
                    tasks[index] = ProcessBatch(batch);
                    tasks[index].Start();
                }
            }

            while (tasks.Any(task => task != null && !task.IsCompleted))
            {
                int index = Task.WaitAny(tasks);
                foreach (var moves in await tasks[index])
                    yield return moves;
            }
        }

        public static bool IsSetOfCycles(IEnumerable<ClassMove> collection)
        {
            var leftMoves = new List<ClassMove>(collection);
            while (leftMoves.Count > 0)
            {
                ClassMove root = leftMoves[leftMoves.Count - 1];
                leftMoves.RemoveAt(leftMoves.Count - 1);
                int currentNumber = root.NumberTo;
                while (currentNumber != root.NumberFrom)
                {
                    int index = leftMoves.FindIndex(move => root.LectureCode == move.LectureCode && currentNumber == move.NumberFrom);
                    if (index == -1)
                        return false;
                    currentNumber = leftMoves[index].NumberTo;
                    leftMoves.RemoveAt(index);
                }
            }
            return true;
        }
    }
}

using KSAGrinder.Statics;

using System;
using System.Collections.Generic;
using System.Linq;

namespace KSAGrinder.Components
{
    public readonly struct ClassMove
    {
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

        public override string ToString() => $"ClassMove {StudentId}, {LectureCode} from {NumberFrom} to {NumberTo}";

        public static IEnumerable<IEnumerable<ClassMove>> GenerateClassMoves(
            IEnumerable<(string StudentId, Schedule Schedule)> targets,
            TradeCapture tradeCapture,
            int depth,
            int maxDepth)
        {
            throw new NotImplementedException();

            if (targets.All(tuple => tradeCapture.GetScheduleOf(tuple.StudentId).Equals(tuple.Schedule)) &&
                tradeCapture.DoesFormTrade() &&
                tradeCapture.AreAllSchedulesValid())
            {
                yield return tradeCapture;
                yield break;
            }

            if (depth > maxDepth) yield break;
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

using KSAGrinder.Statics;

using System.Collections.Generic;

namespace KSAGrinder.Components
{
    public readonly struct ClassMove
    {

        public const int MaxLectureMoves = 1;

        public readonly string StudentId;

        public readonly string Code;

        public readonly int Grade;

        public readonly int NumberFrom, NumberTo;

        public ClassMove(string studentId, string lectureCode, int grade, int numberFrom, int numberTo)
        {
            StudentId = studentId;
            Code = lectureCode;
            Grade = grade;
            NumberFrom = numberFrom;
            NumberTo = numberTo;
        }

        public bool IsValid
            => DataManager.ClassExists(Code, Grade, NumberFrom) && DataManager.ClassExists(Code, Grade, NumberTo);

        public override string ToString() => $"ClassMove {StudentId}, {Code} {DataManager.GetNameOfLectureFromCode(Code)} from {NumberFrom} to {NumberTo}";

        public static bool IsSetOfCycles(IEnumerable<ClassMove> collection)
        {
            List<ClassMove> leftMoves = new(collection);
            while (leftMoves.Count > 0)
            {
                ClassMove root = leftMoves[^1];
                leftMoves.RemoveAt(leftMoves.Count - 1);
                int currentNumber = root.NumberTo;
                while (currentNumber != root.NumberFrom)
                {
                    int index = leftMoves.FindIndex(move
                        => root.Code == move.Code && root.Grade == move.Grade && currentNumber == move.NumberFrom);
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

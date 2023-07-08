using KSAGrinder.Exceptions;
using KSAGrinder.Statics;

using System;
using System.Data;

namespace KSAGrinder.Components
{
    public readonly struct Trade : IEquatable<Trade>
    {
        private static DataSet _data;

        public string Code { get; init; }

        public int Grade { get; init; }

        public string StudentA { get; init; }

        public int NumberA { get; init; }

        public string StudentB { get; init; }

        public int NumberB { get; init; }

        public static void SetData(DataSet data) => _data = data;

        public Trade(string code, int grade, string studentA, int numberA, string studentB, int numberB)
        {
            if (_data is null)
                throw new NoDataException("Data must be provided before any initialization of an instance!");
            Code = code;
            Grade = grade;
            StudentA = studentA;
            NumberA = numberA;
            StudentB = studentB;
            NumberB = numberB;
            if (!IsValid)
                throw new ArgumentException("Arguments are not valid.");
        }

        public Trade(string studentA, in Class classA, string studentB, in Class classB)
        {
            if (_data is null)
                throw new NoDataException("Data must be provided before any initialization of an instance!");
            if (classA.Code != classB.Code || classA.Grade != classB.Grade)
                throw new ArgumentException("Codes and grades of fromClass and toClass must be same!");
            Code = classA.Code;
            Grade = classA.Grade;
            StudentA = studentA;
            NumberA = classA.Number;
            StudentB = studentB;
            NumberB = classB.Number;
            if (!IsValid)
                throw new ArgumentException("Arguments are not valid.");
        }

        private bool IsValid
        {
            get
            {
                if (_data is null)
                    throw new NoDataException("Data must be provided.");
                if (NumberA == NumberB) return false;
                return DataManager.StudentExists(StudentA)
                        && DataManager.StudentExists(StudentB)
                        && DataManager.ClassExists(Code, Grade, NumberA)
                        && DataManager.ClassExists(Code, Grade, NumberB);
            }
        }

        public Trade Inverse => new(Code, Grade, StudentA, NumberB, StudentB, NumberA);

        public override string ToString() => $"{{ Trade {Code} between {StudentA}, {NumberA} and {StudentB}, {NumberB} }}";

        public bool Equals(Trade other)
        {
            if (Code != other.Code) return false;
            (string, int) thisA = (StudentA, NumberA), thisB = (StudentB, NumberB);
            (string, int) otherA = (other.StudentA, other.NumberA), otherB = (other.StudentB, other.NumberB);
            return (thisA == otherA && thisB == otherB) || (thisA == otherB && thisB == otherA);
        }

        public override bool Equals(object? obj)
            => obj is Trade trade && Equals(trade);
    }
}

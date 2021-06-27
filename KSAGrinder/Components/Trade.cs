using KSAGrinder.Exceptions;

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace KSAGrinder.Components
{
    public class Trade
    {
        private static DataSet _data;

        public string LectureCode { get; private set; }

        public string FromStudent { get; private set; }

        public int FromNumber { get; private set; }

        public string ToStudent { get; private set; }

        public int ToNumber { get; private set; }

        public static void SetData(DataSet data) => _data = data;

        public Trade(string lectureCode, string fromStudent, int fromNumber, string toStudent, int toNumber)
        {
            if (_data == null)
                throw new NoDataException("Data must be provided before any initialization of an instance!");
            LectureCode = lectureCode;
            FromStudent = fromStudent;
            FromNumber = fromNumber;
            ToStudent = toStudent;
            ToNumber = toNumber;
            if (!IsValid)
                throw new ArgumentException("Arguments are not valid.");
        }

        public Trade(string fromStudent, Class fromClass, string toStudent, Class toClass)
        {
            if (_data == null)
                throw new NoDataException("Data must be provided before any initialization of an instance!");
            if (fromClass.Code != toClass.Code)
                throw new ArgumentException("Codes of fromClass and toClass must be same!");
            LectureCode = fromClass.Code;
            FromStudent = fromStudent;
            FromNumber = fromClass.Number;
            ToStudent = toStudent;
            ToNumber = toClass.Number;
            if (!IsValid)
                throw new ArgumentException("Arguments are not valid.");
        }

        public bool IsValid
        {
            get
            {
                if (_data == null)
                    throw new NoDataException("Data must be provided.");
                DataTable tStudent = _data.Tables["Student"];
                DataTable tClass = _data.Tables["Class"];
                if (FromNumber == ToNumber) return false;
                object[] certificates = new object[]
                {
                    tStudent.Rows.Find(FromStudent),
                    tStudent.Rows.Find(ToStudent),
                    DataManager.GetClassRow(LectureCode, FromNumber),
                    DataManager.GetClassRow(LectureCode, ToNumber)
                };
                return certificates.All(o => o != null);
            }
        }

        public static void GenerateTrades(string studentID, Schedule original, Schedule target)
        {
            var toTrade = new List<(string Code, int From, int To)>();
            foreach (Class cls1 in original)
            {
                foreach (Class cls2 in target)
                {
                    if (cls1.Code == cls2.Code && cls1.Number != cls2.Number)
                    {
                        toTrade.Add((cls1.Code, cls1.Number, cls2.Number));
                        break;
                    }
                }
            }
        }
    }
}

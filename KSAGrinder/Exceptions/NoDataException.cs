using System;

namespace KSAGrinder.Exceptions
{
    public class NoDataException : Exception
    {
        public NoDataException()
        {

        }

        public NoDataException(string message)
            : base(message)
        {

        }
    }
}

using System;

namespace KSAGrinder.Exceptions
{
    public class DifferentDataSetException : Exception
    {
        public DifferentDataSetException() { }

        public DifferentDataSetException(string message)
            : base(message)
        { }
    }
}

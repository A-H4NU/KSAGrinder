using KSAGrinder.Components;

using System;

namespace KSAGrinder.Exceptions
{
    public class TradeInvalidException : Exception
    {
        public TradeInvalidException(ClassMove trade, bool applying)
            : base($"{trade} is invalid" + (applying ? " while applying it." : "."))
        {
        }
    }
}

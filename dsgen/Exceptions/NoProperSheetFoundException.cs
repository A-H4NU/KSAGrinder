namespace dsgen.Exceptions;

public class NoProperSheetFoundException : Exception
{
    public NoProperSheetFoundException(string? message)
        : base(message) { }
}

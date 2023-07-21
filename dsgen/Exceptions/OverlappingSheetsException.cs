namespace dsgen.Exceptions;

public class OverlappingSheetsException : Exception
{
    public OverlappingSheetsException(string? message)
        : base(message) { }
}

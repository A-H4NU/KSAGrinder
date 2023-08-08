namespace dsgen.Exceptions;

public class DataTableBuilderException : Exception
{
    public DataTableBuilderException() { }

    public DataTableBuilderException(string? message)
        : base(message) { }

    public DataTableBuilderException(string? message, Exception? innerException)
        : base(message, innerException) { }
}

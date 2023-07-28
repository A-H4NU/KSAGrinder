namespace dsgen.Exceptions;

public class TypeException : Exception
{
    public TypeException()
        : base() { }

    public TypeException(string? message)
        : base(message) { }

    public TypeException(string? message, Exception? innerException)
        : base(message, innerException) { }
}

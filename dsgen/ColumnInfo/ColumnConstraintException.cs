namespace dsgen.ColumnInfo;

public class ColumnConstraintException : Exception
{
    public ColumnConstraintException(string? message = null, Exception? innerException = null)
        : base(message, innerException) { }
}

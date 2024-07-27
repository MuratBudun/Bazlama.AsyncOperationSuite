namespace Bazlama.AsyncOperationSuite.Exceptions;

public class QueueFullException : Exception
{
    public QueueFullException(string message) : base(message) { }
}

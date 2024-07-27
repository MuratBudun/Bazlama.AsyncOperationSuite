namespace Bazlama.AsyncOperationSuite.Exceptions;

public class AsyncOperationNotFoundException: Exception
{
    public AsyncOperationNotFoundException(string message) : base(message) { }
}

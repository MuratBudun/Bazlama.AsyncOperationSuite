namespace Bazlama.AsyncOperationSuite.Exceptions;

public class PayloadTypeLimitExceededException : Exception
{
    public PayloadTypeLimitExceededException(string message) : base(message) { }
}
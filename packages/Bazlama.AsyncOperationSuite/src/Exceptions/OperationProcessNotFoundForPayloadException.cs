namespace Bazlama.AsyncOperationSuite.Exceptions;

public class OperationProcessNotFoundForPayloadException : Exception
{
    public OperationProcessNotFoundForPayloadException(string message) : base(message) { }
}
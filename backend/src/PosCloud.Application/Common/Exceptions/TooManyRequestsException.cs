namespace PosCloud.Application.Common.Exceptions;

/// <summary>So'rovlar limiti oshdi, masalan, kod juda tez-tez so'raldi (HTTP 429).</summary>
public class TooManyRequestsException : Exception
{
    public TooManyRequestsException(string message, int? retryAfterSeconds = null) : base(message)
    {
        RetryAfterSeconds = retryAfterSeconds;
    }

    public int? RetryAfterSeconds { get; }
}

namespace PosCloud.Application.Common.Exceptions;

/// <summary>Biznes-qoidasi buzilganda (HTTP 400).</summary>
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}

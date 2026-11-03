namespace PosCloud.Application.Common.Exceptions;

/// <summary>Resurs holati ziddiyatga uchraganda, masalan, username band (HTTP 409).</summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}

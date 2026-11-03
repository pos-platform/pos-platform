namespace PosCloud.Application.Common.Exceptions;

/// <summary>So'ralgan resurs topilmaganda (HTTP 404).</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

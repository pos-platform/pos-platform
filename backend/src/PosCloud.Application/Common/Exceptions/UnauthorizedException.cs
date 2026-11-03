namespace PosCloud.Application.Common.Exceptions;

/// <summary>Autentifikatsiya muvaffaqiyatsiz bo'lganda (HTTP 401).</summary>
public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message) { }
}

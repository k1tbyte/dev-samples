using System.Net;

namespace AccessRefresh.Domain.Exceptions;

public class DomainException(string message, HttpStatusCode statusCode = HttpStatusCode.InternalServerError) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;

    public override string? StackTrace => null;
    
    public static DomainException UserAlreadyExists =>
        new ("User already exists", HttpStatusCode.Conflict);
    
    public static DomainException InvalidAuthToken =>
        new ("Invalid authentication token", HttpStatusCode.Unauthorized);
    
    public static DomainException InvalidCredentials =>
        new ("Invalid credentials", HttpStatusCode.Unauthorized);
    
    public static DomainException CaptchaChallengeFailed =>
        new ("Captcha challenge failed", HttpStatusCode.Forbidden);
}

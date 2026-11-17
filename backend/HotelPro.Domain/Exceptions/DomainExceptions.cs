namespace HotelPro.Domain.Exceptions;

public abstract class DomainException : Exception
{
    public int StatusCode { get; }

    protected DomainException(string message, int statusCode) : base(message)
    {
        StatusCode = statusCode;
    }
}

public sealed class NotFoundException : DomainException
{
    public NotFoundException(string message) : base(message, 404) { }
}

public sealed class ForbiddenException : DomainException
{
    public ForbiddenException(string message = "Access denied.") : base(message, 403) { }
}

public sealed class ConflictException : DomainException
{
    public ConflictException(string message) : base(message, 409) { }
}

public sealed class BusinessRuleException : DomainException
{
    public BusinessRuleException(string message) : base(message, 400) { }
}

public sealed class UnauthorizedDomainException : DomainException
{
    public UnauthorizedDomainException(string message = "Authentication required.") : base(message, 401) { }
}


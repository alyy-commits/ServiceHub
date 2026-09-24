namespace ServiceHub.Domain.Exceptions;

/// <summary>Thrown when a business rule of the domain is violated.</summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}

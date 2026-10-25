namespace DentalClinic.Application.Common.Exceptions;

/// <summary>
/// Provider-neutral signal that a save would violate a unique index. The
/// Infrastructure layer translates provider-specific violations (SQL Server
/// error numbers 2601/2605) into this type, so application handlers can convert
/// it into a domain Result error (409) without depending on a database driver.
/// </summary>
public sealed class UniqueIndexViolationException : Exception
{
    // The message is generic on purpose: it carries no request data and, as a
    // conflict outcome, callers react by re-reading state, not by parsing text.
    public UniqueIndexViolationException(Exception innerException)
        : base("The request would violate a unique index. Reload the current state and try again.", innerException)
    {
    }
}
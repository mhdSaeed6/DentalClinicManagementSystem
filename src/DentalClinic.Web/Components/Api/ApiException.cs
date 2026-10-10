namespace DentalClinic.Web.Api;

/// <summary>
/// Thrown when the backend API returns a non-success status code.
/// Carries the parsed problem details so the UI can display friendly messages
/// and map field level validation errors back to forms.
/// </summary>
public sealed class ApiException : Exception
{
    public ApiException(int statusCode, string message, ProblemDetailsResponse? problem = null)
        : base(message)
    {
        StatusCode = statusCode;
        Problem = problem;
    }

    public int StatusCode { get; }

    public ProblemDetailsResponse? Problem { get; }

    public bool IsUnauthorized => StatusCode == 401 || StatusCode == 403;

    public bool IsNotFound => StatusCode == 404;

    public IReadOnlyDictionary<string, string[]> ValidationErrors =>
        Problem?.Errors ?? new Dictionary<string, string[]>();
}

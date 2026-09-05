namespace DentalClinic.Domain.Common.Results.Abstractions;

public interface IResult
{
    bool IsSuccess { get; }

    List<Error>? Errors { get; }
}

public interface IResult<out T> : IResult
{
    T? Value { get; }
}
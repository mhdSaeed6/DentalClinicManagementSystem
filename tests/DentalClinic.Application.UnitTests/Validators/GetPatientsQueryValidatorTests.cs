using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.Patients.Queries.GetPatients;

using Xunit;

namespace DentalClinic.Application.UnitTests.Validators;

/// <summary>
/// The patient search term is interpolated into the cache key, so an unbounded
/// value lets a client mint unlimited distinct keys. The cap matches the doctor
/// and service queries.
/// </summary>
public class GetPatientsQueryValidatorTests
{
    [Fact]
    public void SearchTerm_WithExactlyTheMaximumLength_IsValid()
    {
        var result = Validate(new string('a', 100));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void SearchTerm_LongerThanTheMaximum_IsRejected()
    {
        var result = Validate(new string('a', 101));

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            error => error.ErrorMessage == "Search term must not exceed 100 characters.");
    }

    [Fact]
    public void SearchTerm_Null_IsValid()
    {
        var result = Validate(null);

        Assert.True(result.IsValid);
    }

    private static FluentValidation.Results.ValidationResult Validate(string? searchTerm) =>
        new GetPatientsQueryValidator().Validate(new GetPatientsQuery(
            PatientAccessScope.None,
            SearchTerm: searchTerm));
}
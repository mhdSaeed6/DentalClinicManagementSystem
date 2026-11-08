using DentalClinic.Domain.Imports;

using Xunit;

namespace DentalClinic.Domain.UnitTests.Imports;

public class ImportJobTests
{
    [Fact]
    public void Create_ShouldMarkCompleted_WhenEveryRowImported()
    {
        var result = ImportJob.Create("  patients.xlsx  ", " admin-1 ", totalRows: 3, importedRows: 3, rejectedRows: 0);

        Assert.True(result.IsSuccess);

        var job = result.Value;
        Assert.Equal("patients.xlsx", job.FileName);
        Assert.Equal("admin-1", job.ImportedBy);
        Assert.Equal(ImportJobStatus.Completed, job.Status);
        Assert.Equal(3, job.TotalRows);
        Assert.Equal(3, job.ImportedRows);
        Assert.Equal(0, job.RejectedRows);
        Assert.Null(job.ErrorReportPath);
        Assert.Empty(job.Errors);
    }

    [Fact]
    public void Create_ShouldMarkFailed_WhenAnyRowRejected()
    {
        var result = ImportJob.Create("patients.xlsx", "admin-1", totalRows: 5, importedRows: 0, rejectedRows: 2);

        Assert.True(result.IsSuccess);
        Assert.Equal(ImportJobStatus.Failed, result.Value.Status);
        Assert.Equal(2, result.Value.RejectedRows);
    }

    [Fact]
    public void Create_ShouldStripDirectoryTraversalFromFileName()
    {
        var result = ImportJob.Create(@"..\..\etc\patients.xlsx", "admin-1", 1, 1, 0);

        Assert.Equal("patients.xlsx", result.Value.FileName);
    }

    [Fact]
    public void Create_ShouldFail_WhenFileNameIsBlank()
    {
        var result = ImportJob.Create("   ", "admin-1", 1, 1, 0);

        Assert.Equal(ImportErrors.FileRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_ShouldFail_WhenImporterIsBlank()
    {
        var result = ImportJob.Create("patients.xlsx", "   ", 1, 1, 0);

        Assert.Equal(ImportErrors.ImportedByRequired.Code, result.TopError.Code);
    }

    [Theory]
    [InlineData(-1, 0, 0)]
    [InlineData(1, -1, 0)]
    [InlineData(1, 0, -1)]
    [InlineData(3, 2, 0)]
    [InlineData(3, 1, 1)]
    [InlineData(3, 4, 0)]
    public void Create_ShouldFail_WithInconsistentCounts(int total, int imported, int rejected)
    {
        var result = ImportJob.Create("patients.xlsx", "admin-1", total, imported, rejected);

        Assert.Equal(ImportErrors.InvalidRowCounts.Code, result.TopError.Code);
    }

    [Fact]
    public void AddErrors_ShouldAttachRowDiagnostics()
    {
        var job = ImportJob.Create("patients.xlsx", "admin-1", 2, 0, 1).Value;
        var error = ImportRowError.Create(job.Id, 3, "PrimaryPhone", "Phone is invalid.", "abc").Value;

        var result = job.AddErrors([error]);

        Assert.True(result.IsSuccess);
        Assert.Single(job.Errors);
        Assert.Same(error, job.Errors[0]);
    }

    [Fact]
    public void AddErrors_ShouldRejectDiagnosticsFromAnotherJob()
    {
        var job = ImportJob.Create("patients.xlsx", "admin-1", 1, 0, 1).Value;
        var other = ImportJob.Create("other.xlsx", "admin-1", 1, 0, 1).Value;
        var error = ImportRowError.Create(other.Id, 2, "Row", "Invalid.").Value;

        var result = job.AddErrors([error]);

        Assert.True(result.IsError);
        Assert.Empty(job.Errors);
    }
}

public class ImportRowErrorTests
{
    [Fact]
    public void Create_ShouldSucceed_AndTrimValues()
    {
        var jobId = Guid.NewGuid();

        var result = ImportRowError.Create(jobId, 4, "  DateOfBirth  ", "  Invalid date.  ", "  31/31/2020  ");

        Assert.True(result.IsSuccess);

        var error = result.Value;
        Assert.Equal(jobId, error.ImportJobId);
        Assert.Equal(4, error.RowNumber);
        Assert.Equal("DateOfBirth", error.Column);
        Assert.Equal("Invalid date.", error.Message);
        Assert.Equal("31/31/2020", error.Value);
    }

    [Fact]
    public void Create_ShouldClampLongValues()
    {
        var value = new string('x', 800);

        var result = ImportRowError.Create(Guid.NewGuid(), 2, "Row", "Invalid.", value);

        Assert.Equal(500, result.Value.Value!.Length);
    }

    [Fact]
    public void Create_ShouldFail_WhenRowNumberIsBelowOne()
    {
        var result = ImportRowError.Create(Guid.NewGuid(), 0, "Row", "Invalid.");

        Assert.Equal("ImportRowError.RowNumberInvalid", result.TopError.Code);
    }

    [Fact]
    public void Create_ShouldFail_WhenMessageIsBlank()
    {
        var result = ImportRowError.Create(Guid.NewGuid(), 1, "Row", "   ");

        Assert.Equal("ImportRowError.MessageRequired", result.TopError.Code);
    }
}
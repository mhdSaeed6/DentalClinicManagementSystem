namespace DentalClinic.Domain.UnitTests.Patients;

using DentalClinic.Domain.Patients;

public class MedicalAlertEvaluatorTests
{
    // ---------------------------------------------------------- Severity

    [Fact]
    public void Evaluate_NullHistory_IsNone()
    {
        Assert.Equal(MedicalAlertSeverity.None, MedicalAlertEvaluator.Evaluate(null));
    }

    [Fact]
    public void Evaluate_EmptyHistory_IsNone()
    {
        Assert.Equal(MedicalAlertSeverity.None, MedicalAlertEvaluator.Evaluate(new MedicalHistory()));
    }

    // Each condition alone is a warning; a bleeding disorder is critical.
    [Fact]
    public void Evaluate_EachConditionAlone_IsWarning()
    {
        var warningSources = new List<MedicalHistory>
        {
            new() { HasDiabetes = true },
            new() { HasHypertension = true },
            new() { HasHeartDisease = true },
            new() { HasEpilepsy = true },
            new() { HasAsthma = true },
            new() { IsPregnant = true, PregnancyWeeks = 32 },
            new() { Allergies = "Penicillin" },
            new() { BloodPressureReading = "not a reading" }, // unparseable recording
        };

        foreach (var history in warningSources)
        {
            Assert.Equal(MedicalAlertSeverity.Warning, MedicalAlertEvaluator.Evaluate(history));
        }
    }

    [Fact]
    public void Evaluate_BleedingDisorder_IsCritical()
    {
        var history = new MedicalHistory
        {
            HasBleedingDisorder = true,
            Allergies = "Penicillin", // even combined with other conditions, stays critical
        };

        Assert.Equal(MedicalAlertSeverity.Critical, MedicalAlertEvaluator.Evaluate(history));
    }

    // ------------------------------------------------- Blood pressure

    [Fact]
    public void Evaluate_NormalBloodPressure_IsNone()
    {
        var history = new MedicalHistory { BloodPressureReading = "118/76" };

        Assert.Equal(MedicalAlertSeverity.None, MedicalAlertEvaluator.Evaluate(history));
    }

    [Theory]
    [InlineData("140/90")] // both at the warning band
    [InlineData("148/92")] // clinical example from the seeded demo data
    [InlineData("139/91")] // diastolic alone carries the warning
    [InlineData("90/120")] // order-independent parts
    public void Evaluate_BloodPressureInWarningBand_IsWarning(string reading)
    {
        var history = new MedicalHistory { BloodPressureReading = reading };

        Assert.Equal(MedicalAlertSeverity.Warning, MedicalAlertEvaluator.Evaluate(history));
    }

    [Theory]
    [InlineData("180/90")] // systolic at the critical line
    [InlineData("200/110")]
    [InlineData("180/120")]
    public void Evaluate_ExtremeBloodPressure_IsCritical(string reading)
    {
        var history = new MedicalHistory { BloodPressureReading = reading };

        Assert.Equal(MedicalAlertSeverity.Critical, MedicalAlertEvaluator.Evaluate(history));
    }

    [Theory]
    [InlineData("120 / 80")] // spaces are tolerated
    [InlineData("120/80 mmHg")] // trailing unit text after the numeric part
    public void Evaluate_NonCanonicalReadingsParse(string reading)
    {
        var history = new MedicalHistory { BloodPressureReading = reading };

        Assert.Equal(MedicalAlertSeverity.None, MedicalAlertEvaluator.Evaluate(history));
    }

    // ------------------------------------------------- Computed property

    [Fact]
    public void AlertSeverity_MatchesEvaluator()
    {
        var history = new MedicalHistory { HasDiabetes = true };

        Assert.Equal(MedicalAlertEvaluator.Evaluate(history), history.AlertSeverity);
    }
}

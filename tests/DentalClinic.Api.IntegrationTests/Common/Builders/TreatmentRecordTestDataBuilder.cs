using DentalClinic.Domain.TreatmentRecords;

namespace DentalClinic.Tests.Common.Builders;

public class TreatmentRecordTestDataBuilder : ITestDataBuilder<TreatmentRecord>
{
    private readonly Guid? _appointmentId = null;
    private Guid _patientId = Guid.NewGuid();
    private Guid _doctorId = Guid.NewGuid();
    private int _toothNumber = 11; // FDI notation
    private string _procedureDetails = "Root Canal Treatment";
    private decimal _cost = 150m;

    public static TreatmentRecordTestDataBuilder Create() => new();

    public TreatmentRecordTestDataBuilder ForPatient(Guid patientId)
    {
        _patientId = patientId;
        return this;
    }

    public TreatmentRecordTestDataBuilder ByDoctor(Guid doctorId)
    {
        _doctorId = doctorId;
        return this;
    }

    public TreatmentRecordTestDataBuilder ForTooth(int toothNumber)
    {
        _toothNumber = toothNumber;
        return this;
    }

    public TreatmentRecordTestDataBuilder WithProcedure(string procedure, decimal cost)
    {
        _procedureDetails = procedure;
        _cost = cost;
        return this;
    }

    public TreatmentRecord Build()
    {
        return TreatmentRecord.Create(
            _patientId,
            _doctorId,
            _toothNumber,
            _procedureDetails,
            _cost,
            _appointmentId).Value;
    }
}
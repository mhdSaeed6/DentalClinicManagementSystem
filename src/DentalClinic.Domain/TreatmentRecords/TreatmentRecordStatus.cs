namespace DentalClinic.Domain.TreatmentRecords;

// حالة سجل العلاج: العناصر المخططة (Planned) لا تُفوترة حتى تُنفّذ (Performed).
// Performed && Cost > 0 is the single billable state (see IsBillable).
// Cancelled items remain in history but are never billable.
// Values start at 1 so CLR default (0) never means "a valid state".
public enum TreatmentRecordStatus
{
    Planned = 1,
    Performed = 2,
    Cancelled = 3
}
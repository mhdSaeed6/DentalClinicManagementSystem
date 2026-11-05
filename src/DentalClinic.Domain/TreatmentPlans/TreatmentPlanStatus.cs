namespace DentalClinic.Domain.TreatmentPlans;

// دورة حياة خطة العلاج: مسودة → نشطة → منجزة/ملغاة.
// Lifecycle: Draft → Active → (Completed | Cancelled). Completed and Cancelled
// are terminal states; values start at 1 so CLR default (0) never means
// "a valid state".
public enum TreatmentPlanStatus
{
    Draft = 1,
    Active = 2,
    Completed = 3,
    Cancelled = 4
}
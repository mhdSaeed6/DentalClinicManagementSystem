namespace DentalClinic.Domain.Common.Enums;

// قيم مضافة فقط (append-only) — القيمة تُخزَّن كرقم صحيح في جدول PatientTeeth؛ ممنوع إعادة الترقيم أو حذف قيمة.
// Int values are append-only: Healthy=0 … Missing=7 (Extracted = removed at a visit, Missing = never erupted).
public enum ToothCondition
{
    Healthy,
    Caries,
    Filled,
    Crown,
    Bridge,
    Implant,
    Extracted,
    Missing
}
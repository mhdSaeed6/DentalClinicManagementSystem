namespace DentalClinic.Domain.Common.Enums;

// قيم مضافة فقط (append-only) — تُخزَّن كرقم صحيح في جدول PatientTeeth.
// Single-surface only for this phase; multi-surface is future work, so values stay append-only ints.
public enum ToothSurface
{
    Occlusal,
    Mesial,
    Distal,
    Buccal,
    Lingual,
    Incisal
}
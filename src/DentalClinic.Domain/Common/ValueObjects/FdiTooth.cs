namespace DentalClinic.Domain.Common.ValueObjects;

// المصدر الوحيد للتحقق من أرقام الأسنان (FDI).
// Single source of truth for FDI tooth validation — every other copy must delegate here, never duplicate the ranges.
public static class FdiTooth
{
    public static bool IsValidToothNumber(int number)
    {
        bool isPermanent = (number >= 11 && number <= 18) ||
                            (number >= 21 && number <= 28) ||
                            (number >= 31 && number <= 38) ||
                            (number >= 41 && number <= 48);

        bool isPediatric = (number >= 51 && number <= 55) ||
                            (number >= 61 && number <= 65) ||
                            (number >= 71 && number <= 75) ||
                            (number >= 81 && number <= 85);

        return isPermanent || isPediatric;
    }
}
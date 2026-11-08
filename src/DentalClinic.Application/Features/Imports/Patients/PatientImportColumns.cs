namespace DentalClinic.Application.Features.Imports.Patients;

/// <summary>
/// The fixed patient-import schema. Header detection is name based (case,
/// spaces, underscores and hyphens are ignored) so a re-ordered or re-labelled
/// export still maps; anything ambiguous can be overridden by an explicit
/// mapping from the preview screen.
/// </summary>
public static class PatientImportColumns
{
    public const string FirstName = "FirstName";
    public const string LastName = "LastName";
    public const string PrimaryPhone = "PrimaryPhone";
    public const string SecondaryPhone = "SecondaryPhone";
    public const string DateOfBirth = "DateOfBirth";
    public const string Gender = "Gender";
    public const string HasWhatsAppOnPrimary = "HasWhatsAppOnPrimary";
    public const string SocialMediaLink = "SocialMediaLink";
    public const string HasDiabetes = "HasDiabetes";
    public const string HasHypertension = "HasHypertension";
    public const string HasHeartDisease = "HasHeartDisease";
    public const string HasBleedingDisorder = "HasBleedingDisorder";
    public const string HasEpilepsy = "HasEpilepsy";
    public const string HasAsthma = "HasAsthma";
    public const string IsPregnant = "IsPregnant";
    public const string PregnancyWeeks = "PregnancyWeeks";
    public const string BloodPressureReading = "BloodPressureReading";
    public const string Allergies = "Allergies";
    public const string MedicalNotes = "MedicalNotes";

    public static readonly IReadOnlyList<PatientImportColumnDefinition> All =
    [
        new(FirstName, "First name", IsRequired: true, ["firstname", "givenname", "الاسم الأول", "الاسم الاول", "الاسم"]),
        new(LastName, "Last name", IsRequired: true, ["lastname", "familyname", "surname", "اسم العائلة", "اسم العائله"]),
        new(PrimaryPhone, "Primary phone", IsRequired: true, ["primaryphone", "phone", "mobile", "phonenumber", "الهاتف", "رقم الهاتف", "الجوال", "الموبايل"]),
        new(DateOfBirth, "Date of birth", IsRequired: true, ["dateofbirth", "dob", "birthdate", "birthday", "تاريخ الميلاد"]),
        new(Gender, "Gender", IsRequired: true, ["gender", "sex", "الجنس"]),
        new(SecondaryPhone, "Secondary phone", IsRequired: false, ["secondaryphone", "alternatephone", "otherphone", "هاتف بديل", "هاتف آخر"]),
        new(HasWhatsAppOnPrimary, "WhatsApp on primary", IsRequired: false, ["haswhatsapponprimary", "whatsapp", "haswhatsapp"]),
        new(SocialMediaLink, "Social media link", IsRequired: false, ["socialmedialink", "socialmedia", "social", "facebook", "instagram"]),
        new(HasDiabetes, "Diabetes", IsRequired: false, ["hasdiabetes", "diabetes", "سكري", "السكري"]),
        new(HasHypertension, "Hypertension", IsRequired: false, ["hashypertension", "hypertension", "bloodpressure", "ضغط", "الضغط"]),
        new(HasHeartDisease, "Heart disease", IsRequired: false, ["hasheartdisease", "heartdisease", "heart", "قلب", "القلب"]),
        new(HasBleedingDisorder, "Bleeding disorder", IsRequired: false, ["hasbleedingdisorder", "bleedingdisorder", "bleeding", "نزيف"]),
        new(HasEpilepsy, "Epilepsy", IsRequired: false, ["hasepilepsy", "epilepsy", "صرع"]),
        new(HasAsthma, "Asthma", IsRequired: false, ["hasasthma", "asthma", "ربو"]),
        new(IsPregnant, "Pregnant", IsRequired: false, ["ispregnant", "pregnant", "pregnancy", "حامل", "الحمل"]),
        new(PregnancyWeeks, "Pregnancy weeks", IsRequired: false, ["pregnancyweeks", "weekspregnant", "أسابيع الحمل"]),
        new(BloodPressureReading, "Blood pressure reading", IsRequired: false, ["bloodpressurereading", "bp", "reading", "قراءة الضغط"]),
        new(Allergies, "Allergies", IsRequired: false, ["allergies", "allergy", "الحساسية"]),
        new(MedicalNotes, "Medical notes", IsRequired: false, ["medicalnotes", "notes", "medicalnote", "ملاحظات", "الملاحظات الطبية"]),
    ];

    public static string Normalize(string value) =>
        new(value
            .Trim()
            .ToLowerInvariant()
            .Where(character => character is not (' ' or '_' or '-'))
            .ToArray());
}

public sealed record PatientImportColumnDefinition(
    string Key,
    string Label,
    bool IsRequired,
    string[] Aliases);

/// <summary>How one target field was resolved against the workbook header row.</summary>
public sealed record PatientImportColumnMapping(
    string Key,
    string Label,
    bool IsRequired,
    string? SourceHeader,
    int ColumnIndex);
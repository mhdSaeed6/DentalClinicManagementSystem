using DentalClinic.Web.Api;

namespace DentalClinic.Web.Components.Shared;

public static class ToothConditionStyle
{
    public static string CssSuffix(ToothCondition condition) => condition switch
    {
        ToothCondition.Healthy => "healthy",
        ToothCondition.Caries => "caries",
        ToothCondition.Filled => "filled",
        ToothCondition.Crown => "crown",
        ToothCondition.Bridge => "bridge",
        ToothCondition.Implant => "implant",
        ToothCondition.Extracted => "extracted",
        ToothCondition.Missing => "missing",
        _ => "unknown",
    };

    public static string Label(ToothCondition condition, bool arabic) => condition switch
    {
        ToothCondition.Healthy => arabic ? "سليم" : "Healthy",
        ToothCondition.Caries => arabic ? "تسوس" : "Caries",
        ToothCondition.Filled => arabic ? "محشو" : "Filled",
        ToothCondition.Crown => arabic ? "تاج" : "Crown",
        ToothCondition.Bridge => arabic ? "جسر" : "Bridge",
        ToothCondition.Implant => arabic ? "زرعة" : "Implant",
        ToothCondition.Extracted => arabic ? "مخلوع" : "Extracted",
        ToothCondition.Missing => arabic ? "مفقود" : "Missing",
        _ => arabic ? "غير معروف" : "Unknown",
    };

    public static string SurfaceLabel(ToothSurface surface, bool arabic) => surface switch
    {
        ToothSurface.Occlusal => arabic ? "إطباقي" : "Occlusal",
        ToothSurface.Mesial => arabic ? "أنسي" : "Mesial",
        ToothSurface.Distal => arabic ? "وحشي" : "Distal",
        ToothSurface.Buccal => arabic ? "دهليزي" : "Buccal",
        ToothSurface.Lingual => arabic ? "لساني" : "Lingual",
        ToothSurface.Incisal => arabic ? "قاطع" : "Incisal",
        _ => arabic ? "غير معروف" : "Unknown",
    };
}

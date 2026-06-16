using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Obstetrics.Helpers;

public static class DefaultUsgTemplateDefinition
{
    public static readonly IReadOnlyList<UsgTemplateRowDto> Rows =
    [
        new() { RowOrder = 1,  WeekLabel = "6 Weeks",  LmpDayOffset = 42,  Activity = "Dating Scan",                     Indication = "Confirm viability and gestational age" },
        new() { RowOrder = 2,  WeekLabel = "10 Weeks", LmpDayOffset = 70,  Activity = "Early Pregnancy Scan",            Indication = "Fetal heart activity and growth" },
        new() { RowOrder = 3,  WeekLabel = "12 Weeks", LmpDayOffset = 84,  Activity = "NT Scan",                         Indication = "Nuchal translucency & combined first-trimester screening" },
        new() { RowOrder = 4,  WeekLabel = "14 Weeks", LmpDayOffset = 98,  Activity = "First Trimester Follow-up",       Indication = "Structural survey and uterine artery Doppler" },
        new() { RowOrder = 5,  WeekLabel = "16 Weeks", LmpDayOffset = 112, Activity = "Mid-Trimester Screening",         Indication = "Quadruple screening and placental location" },
        new() { RowOrder = 6,  WeekLabel = "20 Weeks", LmpDayOffset = 140, Activity = "Anomaly Scan",                    Indication = "Detailed fetal anatomy and morphology survey" },
        new() { RowOrder = 7,  WeekLabel = "24 Weeks", LmpDayOffset = 168, Activity = "Fetal Growth Scan",               Indication = "Biometry, AFI and fetal wellbeing" },
        new() { RowOrder = 8,  WeekLabel = "28 Weeks", LmpDayOffset = 196, Activity = "Growth and Wellbeing",            Indication = "Growth assessment and Doppler studies" },
        new() { RowOrder = 9,  WeekLabel = "32 Weeks", LmpDayOffset = 224, Activity = "Third Trimester Scan",            Indication = "Fetal presentation, growth and liquor volume" },
        new() { RowOrder = 10, WeekLabel = "36 Weeks", LmpDayOffset = 252, Activity = "Pre-Delivery Scan",               Indication = "Presentation, estimated fetal weight and placenta" },
        new() { RowOrder = 11, WeekLabel = "38 Weeks", LmpDayOffset = 266, Activity = "Pre-Labour Assessment",           Indication = "Final wellbeing check before delivery" },
    ];
}

using System.Globalization;

namespace MealShop.Extensions;

public static class DisplayText
{
    public static string ToVnd(this decimal value) => value.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + "đ";

    public static string QualityNote(string? note) => string.IsNullOrWhiteSpace(note)
        ? "Số liệu cần được kiểm tra lại."
        : string.Join("; ", note.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part switch
            {
                "macro_energy_conflict" => "Năng lượng chưa khớp với các chất dinh dưỡng",
                "missing_macros" => "Thiếu số liệu đạm, béo hoặc bột đường",
                "low_energy" => "Năng lượng trong dữ liệu nguồn rất thấp, cần kiểm tra lại",
                _ => part.Contains('_') ? "Số liệu cần được kiểm tra lại" : part
            }));
}

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MealShop.Services;

public static class TextNormalizer
{
    private static readonly Regex MultipleSpacesRegex = new(@"\s+", RegexOptions.Compiled);

    public static string StripAccents(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        // Thay d va D truoc giong logic Python
        var replacedD = text.Replace('đ', 'd').Replace('Đ', 'D');

        // Phan ra FormD de tach dau thanh cac NonSpacingMark
        var normalized = replacedD.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        // Chuyen ve FormC, gop khoang trang, trim va dua ve chu thuong
        var result = sb.ToString().Normalize(NormalizationForm.FormC);
        result = MultipleSpacesRegex.Replace(result, " ");

        return result.Trim().ToLowerInvariant();
    }
}

using System.Globalization;
using MealShop.Extensions;

namespace MealShop.Tests;

public class DisplayTextTests
{
    [Theory]
    [InlineData("en-US")]
    [InlineData("vi-VN")]
    public void Currency_AlwaysUsesVietnameseGrouping(string culture)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            Assert.Equal("35.000đ", 35000m.ToVnd());
            Assert.Equal("0đ", 0m.ToVnd());
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Theory]
    [InlineData("macro_energy_conflict", "Năng lượng chưa khớp")]
    [InlineData("missing_macros", "Thiếu số liệu")]
    [InlineData("low_energy", "Năng lượng trong dữ liệu nguồn rất thấp")]
    public void QualityNote_TranslatesDatasetCodes(string code, string expected)
        => Assert.Contains(expected, DisplayText.QualityNote(code));
}

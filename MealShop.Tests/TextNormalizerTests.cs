using MealShop.Services;

namespace MealShop.Tests;

public class TextNormalizerTests
{
    [Theory]
    [InlineData("Phở bò", "pho bo")]
    [InlineData("Đậu phụ", "dau phu")]
    [InlineData("  Bún  chả ", "bun cha")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData(null, "")]
    [InlineData("Thịt bò xào súp lơ xanh", "thit bo xao sup lo xanh")]
    public void StripAccents_ShouldNormalizeAccentsAndSpacesCorrectly(string? input, string expected)
    {
        var actual = TextNormalizer.StripAccents(input);
        Assert.Equal(expected, actual);
    }
}

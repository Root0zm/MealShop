namespace MealShop.Models;

public class RateLimitingOptions
{
    public int TrackLimit { get; set; } = 10;
    public int CheckoutLimit { get; set; } = 5;
    public int LoginLimit { get; set; } = 10;
}
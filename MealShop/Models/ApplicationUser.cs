using Microsoft.AspNetCore.Identity;

namespace MealShop.Models;

// Nguoi dung he thong ke thua IdentityUser
public class ApplicationUser : IdentityUser
{
    // Ho va ten nguoi dung
    public string FullName { get; set; } = string.Empty;
}

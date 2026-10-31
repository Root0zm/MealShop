using System.ComponentModel.DataAnnotations;

namespace MealShop.ViewModels;

// ViewModel cho form them moi va chinh sua thuc pham / mon an
public class AdminFoodFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Vui long chon loai (ingredient hoac dish).")]
    [RegularExpression("^(ingredient|dish)$", ErrorMessage = "Loai phai la 'ingredient' hoac 'dish'.")]
    [Display(Name = "Loai")]
    public string Kind { get; set; } = "dish";

    [Display(Name = "Dung lam thanh phan")]
    public bool UsableAsComponent { get; set; } = true;

    [Display(Name = "Co the ban tren menu")]
    public bool IsSellable { get; set; } = true;

    [Required(ErrorMessage = "Vui long nhap ten tieng Viet.")]
    [StringLength(255, ErrorMessage = "Ten tieng Viet toi da 255 ky tu.")]
    [Display(Name = "Ten tieng Viet")]
    public string NameVi { get; set; } = string.Empty;

    [StringLength(255, ErrorMessage = "Ten tieng Anh toi da 255 ky tu.")]
    [Display(Name = "Ten tieng Anh")]
    public string? NameEn { get; set; }

    [StringLength(150, ErrorMessage = "Danh muc toi da 150 ky tu.")]
    [Display(Name = "Danh muc")]
    public string? CategoryVi { get; set; }

    // Chi so dinh duong tren 100g (de trong = null, khong tu dien 0)
    [Range(0, 900, ErrorMessage = "Nang luong phai tu 0 den 900 kcal / 100g.")]
    [Display(Name = "Nang luong (kcal)")]
    public decimal? Kcal { get; set; }

    [Range(0, 100, ErrorMessage = "Nuoc phai tu 0 den 100g.")]
    [Display(Name = "Nuoc (g)")]
    public decimal? Water { get; set; }

    [Range(0, 100, ErrorMessage = "Chat dam phai tu 0 den 100g.")]
    [Display(Name = "Chat dam - Protein (g)")]
    public decimal? Protein { get; set; }

    [Range(0, 100, ErrorMessage = "Chat beo phai tu 0 den 100g.")]
    [Display(Name = "Chat beo - Fat (g)")]
    public decimal? Fat { get; set; }

    [Range(0, 100, ErrorMessage = "Chat bot duong phai tu 0 den 100g.")]
    [Display(Name = "Chat bot duong - Carb (g)")]
    public decimal? Carb { get; set; }

    [Range(0, 100, ErrorMessage = "Chat xo phai tu 0 den 100g.")]
    [Display(Name = "Chat xo (g)")]
    public decimal? Fiber { get; set; }

    [Range(0, 10000, ErrorMessage = "Cholesterol phai >= 0.")]
    [Display(Name = "Cholesterol (mg)")]
    public decimal? Cholesterol { get; set; }

    [Range(0, 100000, ErrorMessage = "Canxi phai >= 0.")]
    [Display(Name = "Canxi (mg)")]
    public decimal? Calcium { get; set; }

    [Range(0, 100000, ErrorMessage = "Phot pho phai >= 0.")]
    [Display(Name = "Phot pho (mg)")]
    public decimal? Phosphorus { get; set; }

    [Range(0, 10000, ErrorMessage = "Sat phai >= 0.")]
    [Display(Name = "Sat (mg)")]
    public decimal? Iron { get; set; }

    [Range(0, 100000, ErrorMessage = "Natri phai >= 0.")]
    [Display(Name = "Natri (mg)")]
    public decimal? Sodium { get; set; }

    [Range(0, 100000, ErrorMessage = "Kali phai >= 0.")]
    [Display(Name = "Kali (mg)")]
    public decimal? Potassium { get; set; }

    [Range(0, 10000, ErrorMessage = "Kem phai >= 0.")]
    [Display(Name = "Kem (mg)")]
    public decimal? Zinc { get; set; }

    [Range(0, 100000, ErrorMessage = "Vitamin A phai >= 0.")]
    [Display(Name = "Vitamin A (µg)")]
    public decimal? VitaminA { get; set; }

    [Range(0, 100000, ErrorMessage = "Beta-Carotene phai >= 0.")]
    [Display(Name = "Beta-Carotene (µg)")]
    public decimal? BetaCarotene { get; set; }

    [Range(0, 100000, ErrorMessage = "Vitamin C phai >= 0.")]
    [Display(Name = "Vitamin C (mg)")]
    public decimal? VitaminC { get; set; }

    // Chat luong du lieu
    public string? EnergyFlag { get; set; }
    public string? QaNote { get; set; }
    public bool IsSuspect { get; set; }
}

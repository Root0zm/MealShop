using System.ComponentModel.DataAnnotations;

namespace MealShop.ViewModels;

// Thong tin dang ky tai khoan
public class RegisterViewModel
{
    [Required(ErrorMessage = "Vui long nhap ho va ten.")]
    [StringLength(100, ErrorMessage = "Ho va ten khong duoc qua 100 ky tu.")]
    [Display(Name = "Ho va ten")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui long nhap dia chi email.")]
    [EmailAddress(ErrorMessage = "Dia chi email khong hop le.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui long nhap mat khau.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Mat khau phai co toi thieu {2} ky tu.")]
    [RegularExpression(@"^(?=.*[a-zA-Z])(?=.*\d).+$", ErrorMessage = "Mat khau phai chua ca chu va so.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mat khau")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Xac nhan mat khau")]
    [Compare("Password", ErrorMessage = "Mat khau xac nhan khong khop.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

// Thong tin dang nhap
public class LoginViewModel
{
    [Required(ErrorMessage = "Vui long nhap dia chi email.")]
    [EmailAddress(ErrorMessage = "Dia chi email khong hop le.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui long nhap mat khau.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mat khau")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Ghi nho dang nhap")]
    public bool RememberMe { get; set; }
}

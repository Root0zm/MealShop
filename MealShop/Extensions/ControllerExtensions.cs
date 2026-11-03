using Microsoft.AspNetCore.Mvc;

namespace MealShop.Extensions;

public static class ControllerExtensions
{
    public const string FlashKey = "FLASH_MESSAGE";
    public const string FlashTypeKey = "FLASH_TYPE";

    // Thong bao nhanh (TempData): success, warning, danger
    public static void Flash(this Controller controller, string type, string message)
    {
        controller.TempData[FlashTypeKey] = type;
        controller.TempData[FlashKey] = message;
    }

    public static void FlashSuccess(this Controller controller, string message) => controller.Flash("success", message);
    public static void FlashWarning(this Controller controller, string message) => controller.Flash("warning", message);
    public static void FlashDanger(this Controller controller, string message) => controller.Flash("danger", message);
}
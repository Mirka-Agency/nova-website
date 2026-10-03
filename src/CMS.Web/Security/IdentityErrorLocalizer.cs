using Microsoft.AspNetCore.Identity;

namespace CMS.Web.Security;

/// <summary>Maps ASP.NET Identity English error descriptions to Persian for admin UI.</summary>
public static class IdentityErrorLocalizer
{
    public static string Localize(IdentityError error) => error.Code switch
    {
        "DuplicateUserName" or "DuplicateEmail" => "این ایمیل قبلاً ثبت شده است.",
        "InvalidEmail" => "ایمیل معتبر نیست.",
        "InvalidUserName" => "نام کاربری معتبر نیست.",
        "PasswordTooShort" => "رمز عبور کوتاه است.",
        "PasswordRequiresDigit" => "رمز عبور باید شامل عدد باشد.",
        "PasswordRequiresLower" => "رمز عبور باید شامل حرف کوچک انگلیسی باشد.",
        "PasswordRequiresUpper" => "رمز عبور باید شامل حرف بزرگ انگلیسی باشد.",
        "PasswordRequiresNonAlphanumeric" => "رمز عبور باید شامل یک نویسه غیرحرفی باشد.",
        "PasswordRequiresUniqueChars" => "رمز عبور باید نویسه‌های یکتای بیشتری داشته باشد.",
        "PasswordMismatch" => "رمز عبور فعلی نادرست است.",
        "UserAlreadyHasPassword" => "کاربر از قبل رمز عبور دارد.",
        "UserAlreadyInRole" => "کاربر از قبل در این نقش است.",
        "UserNotInRole" => "کاربر در این نقش نیست.",
        "UserLockoutNotEnabled" => "قفل حساب برای این کاربر فعال نیست.",
        "LoginAlreadyAssociated" => "این ورود از قبل به حساب دیگری متصل است.",
        "InvalidToken" => "توکن نامعتبر است.",
        "ConcurrencyFailure" => "اطلاعات هم‌زمان تغییر کرده است؛ دوباره تلاش کنید.",
        "DefaultError" => "خطایی رخ داد. دوباره تلاش کنید.",
        _ => "خطایی رخ داد. دوباره تلاش کنید."
    };
}

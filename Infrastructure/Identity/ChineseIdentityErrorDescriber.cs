using Microsoft.AspNetCore.Identity;

namespace ExpenseLite.Infrastructure.Identity;

/// <summary>
/// Identity 內建的錯誤訊息是英文的（例如 "Passwords must be at least 8 characters."）。
/// 註冊 / 改密碼失敗時那些字會原封不動出現在畫面上，跟全站的繁體中文夾雜在一起。
/// 這是 Identity 官方的擴充點，只是換句子，不改任何驗證規則。
/// 只覆寫本專案設定下真的碰得到的幾種；沒覆寫的會落回 base 的英文。
/// </summary>
public sealed class ChineseIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DuplicateUserName(string userName)
        => Error(nameof(DuplicateUserName), $"帳號「{userName}」已經有人用了。");

    public override IdentityError DuplicateEmail(string email)
        => Error(nameof(DuplicateEmail), $"Email「{email}」已經有人用了。");

    public override IdentityError InvalidUserName(string? userName)
        => Error(nameof(InvalidUserName), $"帳號「{userName}」含有不允許的字元，請只用英文字母、數字與 - . _ @ +。");

    public override IdentityError InvalidEmail(string? email)
        => Error(nameof(InvalidEmail), $"Email「{email}」格式不正確。");

    public override IdentityError PasswordTooShort(int length)
        => Error(nameof(PasswordTooShort), $"密碼至少要 {length} 個字元。");

    public override IdentityError PasswordRequiresDigit()
        => Error(nameof(PasswordRequiresDigit), "密碼必須包含至少一個數字。");

    public override IdentityError PasswordRequiresLower()
        => Error(nameof(PasswordRequiresLower), "密碼必須包含至少一個小寫英文字母。");

    public override IdentityError PasswordRequiresUpper()
        => Error(nameof(PasswordRequiresUpper), "密碼必須包含至少一個大寫英文字母。");

    public override IdentityError PasswordRequiresNonAlphanumeric()
        => Error(nameof(PasswordRequiresNonAlphanumeric), "密碼必須包含至少一個符號。");

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars)
        => Error(nameof(PasswordRequiresUniqueChars), $"密碼至少要有 {uniqueChars} 種不同的字元。");

    public override IdentityError PasswordMismatch()
        => Error(nameof(PasswordMismatch), "目前的密碼不正確。");

    private static IdentityError Error(string code, string description)
        => new() { Code = code, Description = description };
}

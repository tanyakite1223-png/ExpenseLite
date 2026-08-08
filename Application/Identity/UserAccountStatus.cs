namespace ExpenseLite.Application.Identity;

/// <summary>
/// 帳號的生命週期狀態：主管建立的帳號一出生就是 <see cref="Active"/>，離職或停權則是 <see cref="Disabled"/>。
///
/// 這裡原本有第三個值 <c>Pending</c>（已註冊、尚待主管啟用），隨自助註冊一起拿掉了（2026-08-08）。
/// 它唯一的來源是員工自己註冊出來的帳號，改成主管建帳號之後就沒有東西會產生那個狀態，
/// 留著只會是一個沒有人走的死狀態。「先把帳號建好、過幾天才讓他登入」這個情境經確認不會發生——
/// 實際流程是主管建好帳號、當場把帳號與預設密碼交給本人。
/// </summary>
public enum UserAccountStatus
{
    /// <summary>啟用中，可以登入。</summary>
    Active,

    /// <summary>已停用，不能登入，但歷史紀錄上的姓名仍查得到。</summary>
    Disabled
}

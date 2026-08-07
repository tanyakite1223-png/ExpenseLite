namespace ExpenseLite.Application.Identity;

/// <summary>
/// 帳號的生命週期狀態：自助註冊後是 <see cref="Pending"/>，主管啟用才變 <see cref="Active"/>，
/// 離職或停權則是 <see cref="Disabled"/>。
/// 刻意用三態取代原本的 IsActive bool——bool 分不出「還沒被啟用過」與「用過但被停用」，
/// 登入被擋下來時就講不清楚原因（該說「尚待主管啟用」還是「帳號已停用」）。
/// 放在 Application 而不是 Infrastructure，理由同 <see cref="ExpenseLiteRoles"/>：
/// 這是業務概念，Web 的畫面與 Infrastructure 的 Identity 實作都要用到。
/// </summary>
public enum UserAccountStatus
{
    /// <summary>已註冊、尚待主管啟用，還不能登入。</summary>
    Pending,

    /// <summary>啟用中，可以登入。</summary>
    Active,

    /// <summary>已停用，不能登入，但歷史紀錄上的姓名仍查得到。</summary>
    Disabled
}

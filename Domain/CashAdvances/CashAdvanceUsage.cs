namespace ExpenseLite.Domain.CashAdvances;

/// <summary>
/// 預支款的用途類型，決定「誰的報銷單可以引用這筆錢」。
/// 這是模型層面的區分，不是權限規則能解決的：
/// 一律開放給所有人選，個人預支的錢會被別人報掉；一律只給領款人選，零用金就沒人用得了。
/// </summary>
public enum CashAdvanceUsage
{
    /// <summary>個人預支：主管把錢給某位員工，只有他本人能引用這筆。</summary>
    Personal = 0,

    /// <summary>零用金（共用）：由領款人保管，公司其他人跟他領錢後各自開報銷單引用同一筆。</summary>
    PettyCash = 1
}

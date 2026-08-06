namespace ExpenseLite.Domain.ExpenseReports;

/// <summary>
/// 這張報銷單的錢是從哪裡出的，決定公司事後還要不要付錢給申請人。
/// </summary>
public enum ExpensePaymentMethod
{
    /// <summary>員工墊款：申請人自己的錢先出，核准後公司要付錢還他。</summary>
    EmployeePaid = 0,

    /// <summary>個人預支：公司事先給申請人的錢，用這張單沖抵、多退少補，所以必須綁一筆預支款。</summary>
    PersonalAdvance = 1,

    /// <summary>零用金支付：錢從零用金抽屜出的，申請人當場就拿到現金了，公司不用再付他。只是標記，不綁預支款。</summary>
    PettyCash = 2
}

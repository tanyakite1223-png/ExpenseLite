namespace ExpenseLite.Domain.ExpenseReports;

public enum ExpenseReviewAction
{
    Returned = 1,
    Approved = 2,
    Rejected = 3,

    /// <summary>主管作廢已核准的報銷單。</summary>
    Voided = 4
}

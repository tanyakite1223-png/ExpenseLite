namespace ExpenseLite.Application.CashAdvances;

public sealed record CashAdvanceListItemDto(
    Guid Id,
    string PayeeName,
    string Purpose,
    DateOnly AdvancedAt,
    decimal Amount,
    decimal ApprovedReimbursedAmount,
    decimal Difference,
    bool IsSettled);

public sealed record CashAdvanceOptionDto(
    Guid Id,
    string PayeeName,
    string Purpose,
    DateOnly AdvancedAt,
    decimal Amount,
    decimal ApprovedReimbursedAmount);

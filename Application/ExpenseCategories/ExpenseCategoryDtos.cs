namespace ExpenseLite.Application.ExpenseCategories;

public sealed record ExpenseCategoryListPageDto(
    IReadOnlyList<ExpenseCategoryListItemDto> Categories);

public sealed record ExpenseCategoryListItemDto(
    Guid Id,
    string Name,
    bool IsActive,
    DateTimeOffset CreatedAt);

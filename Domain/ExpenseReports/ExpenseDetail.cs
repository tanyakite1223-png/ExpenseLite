using ExpenseLite.Domain.Shared;
using ExpenseLite.Domain.ValueObjects;

namespace ExpenseLite.Domain.ExpenseReports;

public sealed class ExpenseDetail
{
    private ExpenseDetail()
    {
        Category = string.Empty;
        Description = string.Empty;
        ReceiptType = ExpenseReceiptType.Receipt;
        InvoiceNumber = string.Empty;
        Amount = Money.Zero;
    }

    internal ExpenseDetail(
        DateOnly expenseDate,
        string category,
        string description,
        ExpenseReceiptType receiptType,
        string? invoiceNumber,
        Money amount)
    {
        if (expenseDate == default)
        {
            throw new DomainRuleViolationException("明細日期不可空白。");
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            throw new DomainRuleViolationException("明細類別不可空白。");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomainRuleViolationException("明細說明不可空白。");
        }

        if (receiptType == ExpenseReceiptType.Invoice && string.IsNullOrWhiteSpace(invoiceNumber))
        {
            throw new DomainRuleViolationException("單據類型為發票時，發票號碼必填。");
        }

        Id = Guid.NewGuid();
        ExpenseDate = expenseDate;
        Category = category.Trim();
        Description = description.Trim();
        ReceiptType = receiptType;
        InvoiceNumber = invoiceNumber?.Trim() ?? string.Empty;
        Amount = amount;
    }

    public Guid Id { get; private set; }

    public DateOnly ExpenseDate { get; private set; }

    public string Category { get; private set; }

    public string Description { get; private set; }

    public ExpenseReceiptType ReceiptType { get; private set; }

    public string InvoiceNumber { get; private set; }

    public Money Amount { get; private set; }
}

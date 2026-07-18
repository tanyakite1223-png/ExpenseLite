using ExpenseLite.Domain.ExpenseReports;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseLite.Infrastructure.Persistence;

public sealed class ExpenseReportConfiguration : IEntityTypeConfiguration<ExpenseReport>
{
    public void Configure(EntityTypeBuilder<ExpenseReport> builder)
    {
        builder.ToTable("expense_reports");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.Title)
            .HasColumnName("title")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.ApplicantName)
            .HasColumnName("applicant_name")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.ExpenseType)
            .HasColumnName("expense_type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.ProjectId)
            .HasColumnName("project_id");

        builder.Property(x => x.PaymentMethod)
            .HasColumnName("payment_method")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.CashAdvanceId)
            .HasColumnName("cash_advance_id");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.SubmittedAt)
            .HasColumnName("submitted_at");

        builder.OwnsOne(x => x.TotalAmount, money =>
        {
            money.Property(x => x.Amount)
                .HasColumnName("total_amount")
                .HasColumnType("numeric(18,2)")
                .IsRequired();
        });

        builder.OwnsMany(x => x.Details, detail =>
        {
            detail.ToTable("expense_details");

            detail.WithOwner()
                .HasForeignKey("expense_report_id");

            detail.HasKey(x => x.Id);

            detail.Property(x => x.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            detail.Property<Guid>("expense_report_id")
                .HasColumnName("expense_report_id");

            detail.Property(x => x.ExpenseDate)
                .HasColumnName("expense_date")
                .HasColumnType("date")
                .IsRequired();

            detail.Property(x => x.Category)
                .HasColumnName("category")
                .HasMaxLength(50)
                .IsRequired();

            detail.Property(x => x.Description)
                .HasColumnName("description")
                .HasMaxLength(200)
                .IsRequired();

            detail.Property(x => x.ReceiptType)
                .HasColumnName("receipt_type")
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            detail.Property(x => x.InvoiceNumber)
                .HasColumnName("invoice_number")
                .HasMaxLength(20)
                .IsRequired();

            detail.OwnsOne(x => x.Amount, money =>
            {
                money.Property(x => x.Amount)
                    .HasColumnName("amount")
                    .HasColumnType("numeric(18,2)")
                    .IsRequired();
            });
        });

        builder.Navigation(x => x.TotalAmount)
            .IsRequired();

        builder.Navigation(x => x.Details)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

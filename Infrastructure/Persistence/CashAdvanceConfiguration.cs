using ExpenseLite.Domain.CashAdvances;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseLite.Infrastructure.Persistence;

public sealed class CashAdvanceConfiguration : IEntityTypeConfiguration<CashAdvance>
{
    public void Configure(EntityTypeBuilder<CashAdvance> builder)
    {
        builder.ToTable("cash_advances");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.PayeeName)
            .HasColumnName("payee_name")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Purpose)
            .HasColumnName("purpose")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.AdvancedAt)
            .HasColumnName("advanced_at")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.OwnsOne(x => x.Amount, money =>
        {
            money.Property(x => x.Amount)
                .HasColumnName("amount")
                .HasColumnType("numeric(18,2)")
                .IsRequired();
        });

        builder.Navigation(x => x.Amount)
            .IsRequired();

        builder.OwnsMany(x => x.SettlementRecords, settlement =>
        {
            settlement.ToTable("cash_advance_settlement_records");

            settlement.WithOwner()
                .HasForeignKey("cash_advance_id");

            settlement.HasKey(x => x.Id);

            settlement.Property(x => x.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            settlement.Property<Guid>("cash_advance_id")
                .HasColumnName("cash_advance_id");

            settlement.Property(x => x.SettlementType)
                .HasColumnName("settlement_type")
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            settlement.Property(x => x.SettledAt)
                .HasColumnName("settled_at")
                .HasColumnType("date")
                .IsRequired();

            settlement.OwnsOne(x => x.Amount, money =>
            {
                money.Property(x => x.Amount)
                    .HasColumnName("amount")
                    .HasColumnType("numeric(18,2)")
                    .IsRequired();
            });

            settlement.Property(x => x.HandledBy)
                .HasColumnName("handled_by")
                .HasMaxLength(50)
                .IsRequired();

            settlement.Property(x => x.Note)
                .HasColumnName("note")
                .HasMaxLength(500)
                .IsRequired();

            settlement.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();
        });

        builder.Navigation(x => x.SettlementRecords)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

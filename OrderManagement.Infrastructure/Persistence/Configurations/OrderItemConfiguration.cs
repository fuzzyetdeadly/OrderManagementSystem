using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain.Entities;
using OrderManagement.Infrastructure.Persistence.Constants;

namespace OrderManagement.Infrastructure.Persistence.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        // Index columns with frequent access (scalability)
        builder.HasIndex(oi => oi.OrderId);

        // Set property constraints
        builder.Property(oi => oi.ProductName)
            .HasMaxLength(50)
            .IsRequired();

        builder.ToTable(t => t.HasCheckConstraint(
            DbConstraints.OrderItem.QuantityPositive, "\"Quantity\" > 0")
        );

        // 18, 2 is Standard for money
        builder.Property(oi => oi.UnitPrice).HasPrecision(18, 2);   

        builder.ToTable(t => t.HasCheckConstraint(
            DbConstraints.OrderItem.UnitPricePositive, "\"UnitPrice\" > 0"));
    }
}

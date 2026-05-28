using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Infrastructure.Persistence.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        // Index columns with frequent access (scalability)
        builder.HasIndex(oi => oi.OrderId);

        // Set property constraints
        builder.Property(oi => oi.ProductName).HasMaxLength(200);
        builder.Property(oi => oi.UnitPrice).HasPrecision(18, 2);   // 18, 2 is Standard for money
    }
}

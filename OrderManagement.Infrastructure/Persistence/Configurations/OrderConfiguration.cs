using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        // Index columns with frequent access (scalability)
        builder.HasIndex(o => o.Id);
        builder.HasIndex(o => o.Status);

        // Make status save as string
        builder.Property(o => o.Status).HasConversion<string>();
        builder.Property(o => o.Created).HasDefaultValueSql("NOW()");
    }
}

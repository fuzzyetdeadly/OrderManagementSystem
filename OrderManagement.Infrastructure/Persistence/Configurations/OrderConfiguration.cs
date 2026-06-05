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
        builder.HasIndex(o => o.CustomerId);

        // Make status save as string
        builder.Property(o => o.Status).HasConversion<string>();

        /*
         * Note: For PostgreSQL, "NOW()" was originally used
         * Unfortunately, this doesn't work for SQLite DB tests, which require
         * "CURRENT_TIMESTAMP". Therefore DateTime.UtcNow in Domain
         * is being used in favor of setting a default DB configuration
         * The risk of this in real world scenarios is that sometimes,
         * data gets inserted from other sources, and this can easily be missed.
         * For this demo project, it is considered to be an acceptable trade-off
         * The alternative is test containers, which is not a priority to try now.
         */
        //builder.Property(o => o.Created).HasDefaultValueSql("NOW()");
    }
}

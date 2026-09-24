using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceHub.Domain.Entities;

namespace ServiceHub.Infrastructure.Persistence.Configurations;

public class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.ToTable("Services", t =>
        {
            t.HasCheckConstraint("CK_Services_Price_Positive", "[Price] > 0");
            t.HasCheckConstraint("CK_Services_Duration_Positive", "[DurationInMinutes] > 0");
        });

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).IsRequired().HasMaxLength(100);
        builder.Property(s => s.Description).IsRequired().HasMaxLength(500);
        builder.Property(s => s.DurationInMinutes).IsRequired();
        builder.Property(s => s.Price).IsRequired().HasPrecision(18, 2);
        builder.Property(s => s.IsActive).IsRequired();
        builder.Property(s => s.CreatedAt).IsRequired();

        builder.HasIndex(s => s.Name).IsUnique();
        builder.HasIndex(s => s.IsActive);
    }
}

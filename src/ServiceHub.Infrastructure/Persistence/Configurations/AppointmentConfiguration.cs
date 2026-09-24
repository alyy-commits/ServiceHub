using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceHub.Domain.Entities;

namespace ServiceHub.Infrastructure.Persistence.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments", t =>
        {
            t.HasCheckConstraint("CK_Appointments_EndAfterStart", "[EndDate] > [AppointmentDate]");
        });

        builder.HasKey(a => a.Id);

        builder.Property(a => a.CustomerId).IsRequired();
        builder.Property(a => a.AppointmentDate).IsRequired();
        builder.Property(a => a.EndDate).IsRequired();
        builder.Property(a => a.ReminderSent).IsRequired();
        builder.Property(a => a.CreatedAt).IsRequired();

        // Stored as text ("Pending", "Confirmed"...) so the table is readable in SSMS.
        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasOne(a => a.Customer)
            .WithMany()
            .HasForeignKey(a => a.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Employee)
            .WithMany()
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Service)
            .WithMany()
            .HasForeignKey(a => a.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.CustomerId, a.AppointmentDate });
        builder.HasIndex(a => new { a.EmployeeId, a.AppointmentDate });
        builder.HasIndex(a => new { a.Status, a.AppointmentDate });

        // Safety net against a race condition: two customers booking the exact same start time
        // for the same service at the same moment. Cancelled appointments free the slot.
        builder.HasIndex(a => new { a.ServiceId, a.AppointmentDate })
            .IsUnique()
            .HasFilter("[Status] <> 'Cancelled'");
    }
}

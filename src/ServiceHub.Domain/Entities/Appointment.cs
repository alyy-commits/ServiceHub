using ServiceHub.Domain.Enums;
using ServiceHub.Domain.Exceptions;

namespace ServiceHub.Domain.Entities;

public class Appointment
{
    // Required by EF Core.
    private Appointment()
    {
    }

    public Appointment(string customerId, int serviceId, DateTime startUtc, int durationInMinutes, string? employeeId)
    {
        CustomerId = customerId;
        ServiceId = serviceId;
        EmployeeId = employeeId;
        AppointmentDate = startUtc;
        EndDate = startUtc.AddMinutes(durationInMinutes);
        Status = AppointmentStatus.Pending;
        ReminderSent = false;
        CreatedAt = DateTime.UtcNow;
    }

    public int Id { get; private set; }

    public string CustomerId { get; private set; } = string.Empty;
    public ApplicationUser Customer { get; private set; } = null!;

    public int ServiceId { get; private set; }
    public Service Service { get; private set; } = null!;

    public string? EmployeeId { get; private set; }
    public ApplicationUser? Employee { get; private set; }

    public DateTime AppointmentDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public AppointmentStatus Status { get; private set; }
    public bool ReminderSent { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // ------------- Business rules (state machine) -------------
    // Pending -> Confirmed -> Completed
    // Pending / Confirmed -> Cancelled

    public void Confirm()
    {
        if (Status != AppointmentStatus.Pending)
        {
            throw new DomainException($"Only pending appointments can be confirmed. Current status: {Status}.");
        }

        Status = AppointmentStatus.Confirmed;
        Touch();
    }

    public void Complete()
    {
        if (Status == AppointmentStatus.Cancelled)
        {
            throw new DomainException("A cancelled appointment cannot be completed.");
        }

        if (Status == AppointmentStatus.Completed)
        {
            throw new DomainException("The appointment is already completed.");
        }

        if (Status != AppointmentStatus.Confirmed)
        {
            throw new DomainException("An appointment must be confirmed before it can be completed.");
        }

        Status = AppointmentStatus.Completed;
        Touch();
    }

    public void Cancel()
    {
        if (Status == AppointmentStatus.Completed)
        {
            throw new DomainException("A completed appointment cannot be cancelled.");
        }

        if (Status == AppointmentStatus.Cancelled)
        {
            throw new DomainException("The appointment is already cancelled.");
        }

        Status = AppointmentStatus.Cancelled;
        Touch();
    }

    public void ChangeStatus(AppointmentStatus target)
    {
        switch (target)
        {
            case AppointmentStatus.Confirmed:
                Confirm();
                break;
            case AppointmentStatus.Completed:
                Complete();
                break;
            case AppointmentStatus.Cancelled:
                Cancel();
                break;
            default:
                throw new DomainException("An appointment cannot be moved back to Pending.");
        }
    }

    public void AssignEmployee(string employeeId)
    {
        if (Status == AppointmentStatus.Completed || Status == AppointmentStatus.Cancelled)
        {
            throw new DomainException($"Cannot assign an employee to a {Status.ToString().ToLowerInvariant()} appointment.");
        }

        EmployeeId = employeeId;
        Touch();
    }

    public void MarkReminderSent()
    {
        ReminderSent = true;
    }

    private void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}

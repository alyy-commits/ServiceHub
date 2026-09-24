using Microsoft.AspNetCore.Identity;

namespace ServiceHub.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    // Required by EF Core.
    private ApplicationUser()
    {
    }

    public ApplicationUser(string fullName, string email)
    {
        FullName = fullName;
        Email = email;
        UserName = email;
        EmailConfirmed = true;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public string FullName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }
}

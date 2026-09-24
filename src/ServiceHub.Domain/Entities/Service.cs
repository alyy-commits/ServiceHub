namespace ServiceHub.Domain.Entities;

public class Service
{
    // Required by EF Core.
    private Service()
    {
    }

    public Service(string name, string description, int durationInMinutes, decimal price)
    {
        Name = name;
        Description = description;
        DurationInMinutes = durationInMinutes;
        Price = price;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int DurationInMinutes { get; private set; }
    public decimal Price { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public void Update(string name, string description, int durationInMinutes, decimal price)
    {
        Name = name;
        Description = description;
        DurationInMinutes = durationInMinutes;
        Price = price;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }
}

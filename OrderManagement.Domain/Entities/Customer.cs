namespace OrderManagement.Domain.Entities;

public class Customer
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
    public ICollection<Order> Orders { get; set; } = [];

    public void UpdateDetails(string name, string email)
    {
        Name = name;
        Email = email;
    }
}

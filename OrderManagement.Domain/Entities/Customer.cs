using OrderManagement.Domain.Interfaces;

namespace OrderManagement.Domain.Entities;

// Note: chose to default Name/Email to empty
// Because need to construct this object for deletion
public class Customer : IEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime Updated { get; set; } = DateTime.UtcNow;
    public ICollection<Order> Orders { get; set; } = [];

    public void Update(string name, string email)
    {
        Name = name;
        Email = email;

        Updated = DateTime.UtcNow;
    }
}

namespace OrderManagement.Domain.Entities;

// Note: chose to default Name/Email to empty
// Because need to construct this object for deletion
public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
    public ICollection<Order> Orders { get; set; } = [];

    public void UpdateDetails(string name, string email)
    {
        Name = name;
        Email = email;
    }
}

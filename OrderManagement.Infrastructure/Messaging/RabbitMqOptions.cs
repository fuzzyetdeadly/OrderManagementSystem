namespace OrderManagement.Infrastructure.Messaging;

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; set; } = string.Empty;
    public int Port { get; set; } = -1;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

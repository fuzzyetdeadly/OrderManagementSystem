using Swashbuckle.AspNetCore.Filters;

namespace OrderManagement.API.DTOs;

// Example CreateOrderDto for Swagger
public class CreateOrderDtoExample : IExamplesProvider<CreateOrderDto>
{
    public CreateOrderDto GetExamples() => new()
    {
        CustomerId = 1,
        Items = 
        [
            new CreateOrderItemDto()
            {
                ProductName = "Potato",
                Quantity = 1,
                UnitPrice = 0.99m
            }
        ]
    };
}

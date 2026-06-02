using Microsoft.EntityFrameworkCore;
using OrderManagement.API.DTOs;
using OrderManagement.API.Extensions;
using OrderManagement.Application.Services;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Interfaces;
using OrderManagement.Infrastructure.Persistence;
using OrderManagement.Infrastructure.Repositories;
using Swashbuckle.AspNetCore.Filters;

var builder = WebApplication.CreateBuilder(args);

#region services
// Register DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register repositories/services
// Scoped = One instance per request
// Concrete service, likely won't explore other
// OrderService implementations for this demo project
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<OrderService>();

// Add custom controller support
builder.Services.AddCustomControllers();

// Register Swagger generation service and examples
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.ExampleFilters();   // Instruct Swagger to use examples
});

builder.Services.AddSwaggerExamplesFromAssemblyOf<CreateOrderDtoExample>();
#endregion

#region middleware
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Seed test customer data (if there are none)
    using(var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if(!context.Customers.Any())
        {
            context.Customers.Add(new Customer()
            {
                Name = "Jane Doe",
                Email = "Jane.Doe@gmail.com"
            });
            await context.SaveChangesAsync();
        }
    }
}

app.UseHttpsRedirection();
#endregion

#region endpoints
// Implicitly handles routing and endpoints
app.MapControllers();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");
#endregion

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

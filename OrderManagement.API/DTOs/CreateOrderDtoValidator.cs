using FluentValidation;
using OrderManagement.API.Constants;

namespace OrderManagement.API.DTOs;

public class CreateOrderDtoValidator : AbstractValidator<CreateOrderDto>
{
    public CreateOrderDtoValidator()
    {
        RuleFor(o => o.CustomerId)
            .NotNull()
            .GreaterThan(0).WithMessage(Errors.Order.InvalidCustomerId);

        RuleFor(o => o.Items)
            .NotEmpty().WithMessage(Errors.Order.NoItems);

        RuleForEach(o => o.Items)
            .SetValidator(oi => new CreateOrderItemDtoValidator());
    }
}

public class CreateOrderItemDtoValidator : AbstractValidator<CreateOrderItemDto>
{
    public CreateOrderItemDtoValidator()
    {
        RuleFor(oi => oi.ProductName)
            .NotEmpty()
            .MaximumLength(50).WithMessage(Errors.OrderItem.InvalidProductNameLength);

        RuleFor(oi => oi.Quantity)
            .NotNull()
            .GreaterThan(0).WithMessage(Errors.OrderItem.InvalidQuantity);

        RuleFor(oi => oi.UnitPrice)
            .NotNull()
            .GreaterThan(0.00m).WithMessage(Errors.OrderItem.InvalidUnitPrice);
    }
}
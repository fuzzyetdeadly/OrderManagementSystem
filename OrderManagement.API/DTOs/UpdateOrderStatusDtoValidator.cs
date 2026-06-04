using FluentValidation;

namespace OrderManagement.API.DTOs;

public class UpdateOrderStatusDtoValidator : AbstractValidator<UpdateOrderStatusDto>
{
    public UpdateOrderStatusDtoValidator()
    {
        // Note: 'IsInEnum' didn't work here, because the API accepts enum as string
        // If an invalid string is passed, the conversion will fail.
        // This is handled globally by 'ControllerExtensions.cs'
        // I chose not to attach a '[JsonSerializer]' to the DTO enum as a workaround
        // As it introduces another class of problems (invalid becomes null)
        RuleFor(o => o.Status)
            .NotNull();
    }
}

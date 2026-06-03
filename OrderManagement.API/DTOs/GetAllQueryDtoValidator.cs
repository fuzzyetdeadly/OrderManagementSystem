using OrderManagement.API.Constants;
using FluentValidation;

namespace OrderManagement.API.DTOs;

public class GetAllQueryDtoValidator : AbstractValidator<GetAllQueryDto>
{
    public GetAllQueryDtoValidator()
    {
        RuleFor(q => q.Page)
            .GreaterThan(0);

        RuleFor(q => q.PageSize)
            .GreaterThan(1)
            .LessThanOrEqualTo(100);
    }
}

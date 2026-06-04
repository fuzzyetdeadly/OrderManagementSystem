using ErrorOr;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.API.Constants;
using OrderManagement.API.DTOs;
using OrderManagement.Application.Models;
using OrderManagement.Application.Services;

namespace OrderManagement.API.Controllers;

/* Note:
 * ApiController
  1. Automatic model validation. Removes a lot of boilerplate code
	 If required fields missing, will toss 400 bad request
  2. Auto-bind body parameters. Anything from body gets mapped to request DTOs
  3. Detailed problem responses in standard ProblemDetails shape.	
  4. Route attribute is required.
 */
[ApiController]
[Route("api/orders")]
public class OrdersController : ApiControllerBase
{
    private readonly OrderService _orderService;

    // Validators
    private readonly IValidator<GetAllQueryDto> _getAllValidator;
    private readonly IValidator<CreateOrderDto> _createValidator;
    private readonly IValidator<UpdateOrderStatusDto> _updateStatusValidator;

    public OrdersController(OrderService orderService,
        IValidator<GetAllQueryDto> getAllValidator,
        IValidator<CreateOrderDto> createValidator, 
        IValidator<UpdateOrderStatusDto> updateStatusValidator)
    {
        _orderService = orderService;

        _getAllValidator = getAllValidator;
        _createValidator = createValidator;
        _updateStatusValidator = updateStatusValidator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetAllQueryDto dto)
    {
        // Validate the DTO before any operations
        var outcome = await _getAllValidator.ValidateAsync(dto);
        if (!outcome.IsValid)
            return ValidationProblem(new ValidationProblemDetails(outcome.ToDictionary()));

        var orders = await _orderService.GetAllAsync(dto.Page, dto.PageSize);

        return Ok(orders);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _orderService.GetOrderIdAsync(id);

        // 'Match' accepts delegates that fire based on whether
        // the result returned a valid response object or error
        return result.Match(
                order => Ok(order),
                errors => Problem(errors)
            );
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
    {
        // Validate the DTO before any operations
        var outcome = await _createValidator.ValidateAsync(dto);
        if (!outcome.IsValid)
            return ValidationProblem(new ValidationProblemDetails(outcome.ToDictionary()));

        // Map DTO to OrderRequest (no null values)
        var orderRequest = new CreateOrderRequest(
            dto.CustomerId!.Value, 
            [.. dto.Items.Select(oi => 
                new CreateOrderItemRequest(
                    oi.ProductName!, 
                    oi.Quantity!.Value, 
                    oi.UnitPrice!.Value))]
        );

        var result = await _orderService.CreateAsync(orderRequest);

        /* Note:
         * OnSuccess: Sets Status = 201 and the location header to
         * Location: https://<host>/api/orders/{id}
         * Returns 'createdOrder' in the response
         * Mind: location is a bit redundant, since response already returns dto
         * this pattern is more about following REST conventions
         * In older web dev, apparently Location is used to trigger a client callback
         * to GET the location.
         * OnFailure: Return error response
         */
        return result.Match(
                order => CreatedAtAction(nameof(GetById), new { id = order.Id }, order),
                errors => Problem(errors)
            );
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusDto dto)
    {
        // Validate the DTO before any operations
        var outcome = await _updateStatusValidator.ValidateAsync(dto);
        if (!outcome.IsValid)
            return ValidationProblem(new ValidationProblemDetails(outcome.ToDictionary()));

        var result = await _orderService.UpdateStatusAsync(id, dto.Status!.Value);

        return result.Match(
            order => Ok(order),
            errors => Problem(errors)
        );
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _orderService.DeleteAsync(id);

        // Status 204 on successful deletion (no content)
        // Explicit 'IActionResult' generic needed because
        // for 'NoContent', Match couldn't infer it
        return result.Match<IActionResult>(
            deleted => NoContent(),
            errors => Problem(errors)
        );
    }
}

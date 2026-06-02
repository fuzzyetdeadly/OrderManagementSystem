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
public class OrdersController : ControllerBase
{
    private readonly OrderService _orderService;

    public OrdersController(OrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetAllQueryDto dto)
    {
        var orders = await _orderService.GetAllAsync(dto.Page, dto.PageSize);

        return Ok(orders);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var order = await _orderService.GetOrderIdAsync(id);

        return order != null ? Ok(order) :
            Problem(
                title: Errors.Order.NotFound,
                detail: Errors.Order.NotFoundDetail(id),
                statusCode: StatusCodes.Status404NotFound
            );
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
    {
        // Map DTO to OrderRequest (no null values)
        var orderRequest = new CreateOrderRequest(
            dto.CustomerId!.Value, 
            [.. dto.Items.Select(i => 
                new CreateOrderItemRequest(i.ProductName, i.Quantity, i.UnitPrice))]
        );

        var createdOrder = await _orderService.CreateAsync(orderRequest);

        /* Note:
         * Sets Status = 201 and the location header to
         * Location: https://<host>/api/orders/{id}
         * Returns 'createdOrder' in the response
         * Mind: location is a bit redundant, since response already returns dto
         * this pattern is more about following REST conventions
         * In older web dev, apparently Location is used to trigger a client callback
         * to GET the location.
         */
        return CreatedAtAction(nameof(GetById), new { id = createdOrder.Id }, createdOrder);
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusDto dto)
    {
        // Status is guarenteed not null here, as [Required] validates it's presence
        // [ApiController] will automatically reject with status 400 if validation failed
        var updatedOrder = await _orderService.UpdateStatusAsync(id, dto.Status!.Value);

        return updatedOrder != null ? Ok(updatedOrder) :
            Problem(
                title: Errors.Order.NotFound,
                detail: Errors.Order.NotFoundDetail(id),
                statusCode: StatusCodes.Status404NotFound
            );
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var isDeleted = await _orderService.DeleteAsync(id);

        // Status 204 on successful deletion (no content)
        return isDeleted ? NoContent() : 
            Problem(
                title: Errors.Order.NotFound,
                detail: Errors.Order.NotFoundDetail(id),
                statusCode: StatusCodes.Status404NotFound
            );
    }
}

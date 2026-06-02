using System.ComponentModel.DataAnnotations;

namespace OrderManagement.API.DTOs;

using OrderManagement.API.Constants;

public class GetAllQueryDto
{
    [Range(1, int.MaxValue, ErrorMessage = Errors.General.InvalidPage)]
    public int Page { get; set; } = 1;
    [Range(1, 100, ErrorMessage = Errors.General.InvalidPageNumber)]
    public int PageSize { get; set; } = 20;
}

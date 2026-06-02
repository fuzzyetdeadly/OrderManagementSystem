namespace OrderManagement.API.Constants;

// Separate class/inner classes for errors to for code maintainability/separation of concerns
public static class Errors
{
    public static class General
    {
        public const string InvalidPage = "Page must be > 0";
        public const string InvalidPageNumber = "PageSize must be between 1-100";
        public const string InvalidValue = "The provided value is invalid";
    }

    public static class Order
    {
        public const string InvalidCustomerId = "CustomerId must be > 0";
        public const string NotFound = "Order not found";
        public static string NotFoundDetail(int id) => $"Order with ID {id} was not found";
    }

    public static class OrderItem
    {
        public const string InvalidProductNameLength = "ProductName must be between 1-50 chars";
        public const string InvalidQuantity = "Quantity must be > 0";
        public const string InvalidUnitPrice = "UnitPrice must be > 0";
    }
}

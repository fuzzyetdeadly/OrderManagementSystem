namespace OrderManagement.API.Constants;

// Separate class/inner classes for errors to for code maintainability/separation of concerns
public static class ErrorMessages
{
    public static class General
    {
        public const string InvalidValue = "The provided value is invalid";
    }

    public static class Order
    {
        public const string InvalidCustomerId = "CustomerId must be > 0";
    }

    public static class OrderItem
    {
        public const string InvalidProductNameLength = "ProductName must be 1-50 chars";
        public const string InvalidQuantity = "Quantity must be > 0";
        public const string InvalidUnitPrice = "UnitPrice must be > 0";
    }
}

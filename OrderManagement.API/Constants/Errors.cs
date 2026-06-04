namespace OrderManagement.API.Constants;

// Separate class/inner classes for errors to for code maintainability/separation of concerns
public static class Errors
{
    public static class General
    {
        public const string InvalidValue = "The provided value is invalid";
        public const string UnexpectedError = "An unexpected error occurred";
    }

    public static class Customer
    {
        public const string NotFound = "The requested customer doesn't exist";
    }

    public static class Order
    {
        public const string InvalidCustomerId = "CustomerId must be > 0";
        public const string NoItems = "Order must contain at least one item";
        public const string NotFound = "The requested order doesn't exist";
    }

    public static class OrderItem
    {
        public const string InvalidProductNameLength = "ProductName must be between 1-50 chars";
        public const string InvalidQuantity = "Quantity must be > 0";
        public const string InvalidUnitPrice = "UnitPrice must be > 0";
    }
}

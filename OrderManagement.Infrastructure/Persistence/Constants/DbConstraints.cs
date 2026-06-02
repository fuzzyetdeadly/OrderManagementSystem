namespace OrderManagement.Infrastructure.Persistence.Constants;

public static class DbConstraints
{
    public static class OrderItem
    {
        public const string QuantityPositive = "CK_OrderItem_Quantity";
        public const string UnitPricePositive = "CK_OrderItem_UnitPrice";
    }
}

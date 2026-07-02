import type { Order, OrderItem } from "../../types/order";

// Note: Partial<Order> allows partial overrides of properties
// 'id' collisions should be overridden as necessary in tests
// rolling id negatively impacts test maintainability
export function makeOrder(overrides: Partial<Order> = {}): Order {
  return {
    id: 1,
    status: "Pending",
    created: "2023-01-01T12:00:00Z" /* don't care */,
    customerId: 1,
    items: [makeOrderItem()] /* intentionally not merged with overrides */,
    ...overrides,
  };
}

export function makeOrderItem(overrides: Partial<OrderItem> = {}): OrderItem {
  return {
    productName: "Potato",
    quantity: 1,
    unitPrice: 0.99,
    ...overrides,
  };
}

import type { Order } from "../../types/order";

// Note: Partial<Order> allows partial overrides of properties
export const makeOrder = (overrides: Partial<Order> = {}): Order => ({
  id: 1,
  status: "Pending",
  created: "2023-01-01T12:00:00Z" /* don't care */,
  customerId: 1,
  items: [{ productName: "Potato", quantity: 1, unitPrice: 0.99 }],
  ...overrides,
});

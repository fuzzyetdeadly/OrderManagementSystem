// 'as const' fixes values in array, prevents widening to string[]
export const ORDER_STATUSES = [
  "Pending",
  "Processing",
  "Delivered",
  "Completed",
  "Cancelled",
] as const;

export type OrderStatus = (typeof ORDER_STATUSES)[number];

export type Order = {
  id: number;
  status: OrderStatus;
  created: string;
  customerId: number;
  items: OrderItem[];
};

export type OrderItem = {
  productName: string;
  quantity: number;
  unitPrice: number;
};

export type CreateOrderPayload = {
  customerId: number;
  items: OrderItem[];
};

export type UpdateOrderStatusPayload = {
  status: string;
};

import { api } from "./client";
import type {
  Order,
  CreateOrderPayload,
  UpdateOrderStatusPayload,
} from "../types/order";
import type { PaginationOptions } from "../types/common";

export const getOrders = async (
  params: PaginationOptions = {},
): Promise<Order[]> => {
  const response = await api.get<Order[]>("/orders", { params });

  return response.data;
};

export const createOrder = async (
  payload: CreateOrderPayload,
): Promise<Order> => {
  const response = await api.post<Order>("/orders", payload);

  return response.data;
};

export const updateOrderStatus = async (
  id: number,
  payload: UpdateOrderStatusPayload,
): Promise<Order> => {
  const response = await api.patch<Order>(`/orders/${id}/status`, payload);

  return response.data;
};

export const deleteOrder = async (id: number): Promise<void> => {
  await api.delete(`/orders/${id}`);
};

import { api } from "./client";
import type { Order, CreateOrderPayload } from "../types/order";
import type { PaginationOptions } from "../types/common";

export const getOrders = async(
	params: PaginationOptions = {}
): Promise<Order[]> => {
	const response = await api.get<Order[]>("/orders", { params });
	
	return response.data
}

export const createOrder = async (payload: CreateOrderPayload): Promise<Order> => {
	const response = await api.post<Order>("/orders", payload);
	
	return response.data;
}
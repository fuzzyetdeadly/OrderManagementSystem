import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { getOrders, createOrder, updateOrderStatus, deleteOrder } from "../api/orders";
import type { Order, CreateOrderPayload, UpdateOrderStatusPayload } from "../types/order";

export function useOrders() {
	const queryClient = useQueryClient();
	
	const ordersQuery = useQuery({
		queryKey: ["orders"],
		queryFn: getOrders
	});
	
	// Note: all mutations have prev: Order[] = []
	// to guard in case the prev cached value is undefined
	const createMutation = useMutation({
		mutationFn: (payload: CreateOrderPayload) =>
			createOrder(payload),
		onSuccess: (created: Order) => {
			queryClient.setQueryData(["orders"], (prev: Order[] = []) =>
				[...prev, created]
			);
		}
	});
	
	const updateStatusMutation = useMutation({
		mutationFn: ({ id, payload }: { id: number; payload: UpdateOrderStatusPayload }) =>
			updateOrderStatus(id, payload),
		onSuccess: (updated: Order) => {
			queryClient.setQueryData(["orders"], (prev: Order[] = []) =>
				prev.map((order) => (order.id === updated.id ? updated : order))
			);
		}
	});
	
	const deleteMutation = useMutation({
		mutationFn: (id: number) => deleteOrder(id),
		onSuccess: (_, id: number) => {
			// Note: id is captured from mutation variables (second arg)
			queryClient.setQueryData(["orders"], (prev: Order[] = []) =>
				prev.filter((order) => order.id !== id)
			);
		}
	});
	
	return {
		ordersQuery,
		
		createOrder: (order: CreateOrderPayload) =>
			createMutation.mutateAsync(order),
		createMutation,
		
		updateOrderStatus: (id: number, payload: UpdateOrderStatusPayload) =>
			updateStatusMutation.mutateAsync({id, payload}),
		updateStatusMutation,
		
		deleteOrder: (id: number) => deleteMutation.mutateAsync(id),
		deleteMutation
	};
}
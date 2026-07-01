import { renderHook, waitFor } from "@testing-library/react";
import { createQueryWrapper } from "../test/queryWrapper";
import { useOrders } from "./useOrders";
import { makeOrder } from "../mocks/factories/orderFactory";
import type {
  Order,
  CreateOrderPayload,
  UpdateOrderStatusPayload,
} from "../types/order";

describe("useOrders", () => {
  it("fetches orders on mount", async () => {
    const { wrapper } = createQueryWrapper();
    const { result } = renderHook(() => useOrders(), { wrapper });

    await waitFor(() => {
      expect(result.current.ordersQuery.isSuccess).toBe(true);
    });

    expect(result.current.ordersQuery.data).toEqual([makeOrder()]);
  });

  it("creates order in cache", async () => {
    const { wrapper, queryClient } = createQueryWrapper();
    const { result } = renderHook(() => useOrders(), { wrapper });

    // Wait for initial fetch to complete
    await waitFor(() => {
      expect(result.current.ordersQuery.isSuccess).toBe(true);
    });

    // Create a new order, items are don't care for this test
    const payload: CreateOrderPayload = {
      customerId: 1,
      items: [],
    };

    await result.current.createOrder(payload);

    // Wait cache to be updated, then assert
    await waitFor(() => {
      const cached = queryClient.getQueryData(["orders"]);

      expect(cached).toBeDefined(); // defensive check
      expect(cached).toHaveLength(2);
    });
  });

  it("updates only matching order in cache", async () => {
    const { wrapper, queryClient } = createQueryWrapper();
    const { result } = renderHook(() => useOrders(), { wrapper });

    // Wait for initial fetch to complete
    await waitFor(() => {
      expect(result.current.ordersQuery.isSuccess).toBe(true);
    });

    // Seed additional order to allow no match test
    queryClient.setQueryData<Order[]>(["orders"], (prev: Order[] = []) => [
      ...prev,
      makeOrder({ id: 2 }),
    ]);

    // Update an existing order's status
    // Note: happy flow always returns updated order as "Processing"
    const id = 1;
    const payload: UpdateOrderStatusPayload = { status: "Completed" };

    await result.current.updateOrderStatus(id, payload);

    // Wait cache to be updated, then assert
    await waitFor(() => {
      const cached = queryClient.getQueryData<Order[]>(["orders"]);

      expect(cached).toBeDefined(); // defensive check
      expect(cached).toHaveLength(2);

      // Only order with matching id should have changed status
      expect(cached!.find((o) => o.id === id)?.status).toBe(payload.status);
      expect(cached!.find((o) => o.id === 2)?.status).not.toBe(payload.status);
    });
  });

  it("deletes only matching order in cache", async () => {
    const { wrapper, queryClient } = createQueryWrapper();
    const { result } = renderHook(() => useOrders(), { wrapper });

    // Wait for initial fetch to complete
    await waitFor(() => {
      expect(result.current.ordersQuery.isSuccess).toBe(true);
    });

    // Seed additional order to allow no match test
    queryClient.setQueryData<Order[]>(["orders"], (prev: Order[] = []) => [
      ...prev,
      makeOrder({ id: 2 }),
    ]);

    // Delete an existing order
    const id = 1;

    await result.current.deleteOrder(id);

    // Wait cache to be updated, then assert
    await waitFor(() => {
      const cached = queryClient.getQueryData<Order[]>(["orders"]);

      expect(cached).toBeDefined(); // defensive check
      expect(cached).toHaveLength(1);

      // Only order with matching id should have been removed
      expect(cached!.find((o) => o.id === id)).toBeUndefined();
      expect(cached!.find((o) => o.id === 2)).toBeDefined();
    });
  });
});

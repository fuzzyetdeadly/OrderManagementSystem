import { renderHook, waitFor } from "@testing-library/react";
import { useOrders } from "./useOrders";
import { createQueryWrapper } from "../test-utils";
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

  it("caches created order on success", async () => {
    const { wrapper, queryClient } = createQueryWrapper();
    const { result } = renderHook(() => useOrders(), { wrapper });

    // Wait for initial fetch to complete
    await waitFor(() => {
      expect(result.current.ordersQuery.isSuccess).toBe(true);
    });

    // Create a new order
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

  it("caches updated order status on success", async () => {
    const { wrapper, queryClient } = createQueryWrapper();
    const { result } = renderHook(() => useOrders(), { wrapper });

    // Wait for initial fetch to complete
    await waitFor(() => {
      expect(result.current.ordersQuery.isSuccess).toBe(true);
    });

    // Update an existing order's status
    // Note: happy flow always returns updated order as "Processing"
    const id = 1;
    const payload: UpdateOrderStatusPayload = { status: "Processing" };

    await result.current.updateOrderStatus(id, payload);

    // Wait cache to be updated, then assert
    await waitFor(() => {
      const cached = queryClient.getQueryData<Order[]>(["orders"]);

      expect(cached).toBeDefined(); // defensive check
      expect(cached).toHaveLength(1);
      expect(cached![0].status).toBe(payload.status);
    });
  });

  it("deletes order from cache on success", async () => {
    const { wrapper, queryClient } = createQueryWrapper();
    const { result } = renderHook(() => useOrders(), { wrapper });

    // Wait for initial fetch to complete
    await waitFor(() => {
      expect(result.current.ordersQuery.isSuccess).toBe(true);
    });

    // Delete an existing order
    const id = 1;

    await result.current.deleteOrder(id);

    // Wait cache to be updated, then assert
    await waitFor(() => {
      const cached = queryClient.getQueryData<Order[]>(["orders"]);

      expect(cached).toBeDefined(); // defensive check
      expect(cached).toHaveLength(0);
    });
  });
});

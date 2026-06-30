import { renderHook, waitFor } from "@testing-library/react";
import { useOrders } from "./useOrders";
import { createQueryWrapper } from "../test-utils";
import { makeOrder } from "../mocks/factories/orderFactory";

describe("useOrders", () => {
  it("fetches orders on mount", async () => {
    const { wrapper } = createQueryWrapper();
    const { result } = renderHook(() => useOrders(), { wrapper });

    await waitFor(() => {
      expect(result.current.ordersQuery.isSuccess).toBe(true);
    });

    expect(result.current.ordersQuery.data).toEqual([makeOrder()]);
  });
});

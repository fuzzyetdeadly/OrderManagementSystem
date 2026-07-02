import { useOrders } from "../../hooks/useOrders";

// Creates a mock 'useOrders' hook for testing
// Accepts function handle overrides to support call and return assertions
export function createUseOrdersMock(
  overrides: Partial<ReturnType<typeof useOrders>> = {},
): ReturnType<typeof useOrders> {
  // Merge 'ordersQuery' overrides with default mock behavior
  const { ordersQuery, ...rest } = overrides;

  return {
    ordersQuery: createOrdersQueryMock(ordersQuery),
    createOrder: vi.fn(),
    updateOrderStatus: vi.fn(),
    deleteOrder: vi.fn(),
    ...rest,
  };
}

// Creates a mock 'ordersQuery' function for testing
// Helper method to mock ordersQuery behavior in tests
// Note that "as unknown" casts away type safety, but fixes
// a parser error with this function call above
export function createOrdersQueryMock(
  overrides: Partial<ReturnType<typeof useOrders>["ordersQuery"]> = {},
) {
  return {
    data: [],
    isLoading: false,
    isError: false,
    error: null,
    ...overrides,
  } as unknown as ReturnType<typeof useOrders>["ordersQuery"];
}

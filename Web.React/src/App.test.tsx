import { render, screen } from "@testing-library/react";
import { makeOrder } from "./test/factories/orderFactory";
import {
  createOrdersQueryMock,
  createUseOrdersMock,
} from "./test/factories/useOrdersFactory";
import type { Order } from "./types/order";
import { useOrders } from "./hooks/useOrders";
import App from "./App";
import OrderForm from "./components/OrderForm";
import OrderList from "./components/OrderList";

// Mock hooks and components to keep tests focused on App component
vi.mock("./hooks/useOrders");
vi.mock("./components/OrderForm", () => {
  return { default: vi.fn(() => <div data-testid="order-form" />) };
});
vi.mock("./components/OrderList", () => {
  return { default: vi.fn(() => <div data-testid="order-list" />) };
});

// Default orders for testing
const orders: Order[] = [makeOrder({ id: 1 }), makeOrder({ id: 2 })];

beforeEach(() => {
  // Reset 'useOrders' mock before each test to ensure clean state
  vi.mocked(useOrders).mockReturnValue(
    createUseOrdersMock({
      ordersQuery: createOrdersQueryMock({ data: orders }),
    }),
  );
});

describe("App", () => {
  it("invokes hooks and renders components correctly", () => {
    render(<App />);

    // Hooks
    expect(useOrders).toHaveBeenCalledOnce();

    // Visuals
    const heading = screen.getByRole("heading", {
      level: 1,
      name: "Order Management",
    });

    expect(heading).toBeInTheDocument();

    // Components
    expect(OrderForm).toHaveBeenCalledOnce();
    expect(OrderList).toHaveBeenCalledWith({ orders }, undefined);
  });

  it("handles undefined data from ordersQuery gracefully", () => {
    // Override 'useOrders' mock to return undefined data
    vi.mocked(useOrders).mockReturnValue(
      createUseOrdersMock({
        ordersQuery: createOrdersQueryMock({ data: undefined }),
      }),
    );

    render(<App />);

    // Assert that OrderList is called with an empty array when data is undefined
    expect(OrderList).toHaveBeenCalledWith({ orders: [] }, undefined);
  });
});

import { render, screen } from "@testing-library/react";
import { makeOrder } from "../test/factories/orderFactory";
import type { Order } from "../types/order";
import OrderList from "./OrderList";
import OrderRow from "./OrderRow";

// Mock OrderRow to decouple OrderList tests from OrderRow
vi.mock("./OrderRow", () => {
  return { default: vi.fn(() => <tr data-testid="order-row" />) };
});

// Default orders for testing
const orders: Order[] = [makeOrder({ id: 1 }), makeOrder({ id: 2 })];

describe("OrderList", () => {
  it("renders one header row with correct column names", () => {
    render(<OrderList orders={[]} />);

    // Assert that there is exactly one header row with no orders
    const rows = screen.getAllByRole("row");

    expect(rows).toHaveLength(1);

    // Assert that headers have expected names
    const headers = screen
      .getAllByRole("columnheader")
      .map((header) => header.textContent);

    expect(headers).toEqual(["ID", "Customer", "Status", "Items", "Actions"]);
  });

  it("passes props correctly to each OrderRow", () => {
    render(<OrderList orders={orders} />);

    // Collect passed props
    const orderRowProps = vi
      .mocked(OrderRow)
      .mock.calls.map(([prop]) => prop.order);

    // Assert that props matches the orders passed to OrderList
    expect(orderRowProps).toEqual(orders);
  });
});

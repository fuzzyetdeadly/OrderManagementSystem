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

    expect(headers).toEqual([
      "orderList.columns.id",
      "orderList.columns.customer",
      "orderList.columns.status",
      "orderList.columns.items",
      "orderList.columns.action",
    ]);
  });

  it("passes props correctly to each OrderRow", () => {
    render(<OrderList orders={orders} />);

    // Collect passed props
    const orderRowProps = vi
      .mocked(OrderRow)
      .mock.calls.map(([props]) => props);

    // Assert that row orders match the orders passed to OrderList
    expect(orderRowProps.map((prop) => prop.order)).toEqual(orders);

    // Assert that 'isMobile' properly drilled to order row
    // Note: prefer this over check 'every', to make it easier
    // to see what didn't match
    expect(orderRowProps.map((props) => props.isMobile)).toEqual(
      orders.map(() => false),
    );
  });
});

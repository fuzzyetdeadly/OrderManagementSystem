import { render, screen } from "@testing-library/react";
import useMediaQuery from "@mui/material/useMediaQuery";
import { makeOrder } from "../test/factories/orderFactory";
import type { Order } from "../types/order";
import OrderList from "./OrderList";
import OrderRow from "./OrderRow";

// Mock 'useMediaQuery' to control 'isMobile' state
vi.mock("@mui/material/useMediaQuery", () => {
  return { default: vi.fn() };
});

// Mock OrderRow to decouple OrderList tests from OrderRow
vi.mock("./OrderRow", () => {
  return { default: vi.fn(() => <tr data-testid="order-row" />) };
});

// Default orders for testing
const orders: Order[] = [makeOrder({ id: 1 }), makeOrder({ id: 2 })];

describe.each([false, true])("OrderList (isMobile=%s)", (isMobile: boolean) => {
  // Note: chose to set isMobile for each describe.each param in beforeEach;
  // In case vite.config.ts ever sets 'restoreMocks: true'
  // It was a choice not to do any manual mock resets,
  // since the required return value is always set here.
  beforeEach(() => {
    vi.mocked(useMediaQuery).mockReturnValue(isMobile);
  });

  it("renders one header row with correct column names", () => {
    render(<OrderList orders={[]} />);

    // Assert that there is exactly one header row with no orders
    const rows = screen.getAllByRole("row");

    expect(rows).toHaveLength(1);

    // Assert that headers have expected names
    const headers = screen
      .getAllByRole("columnheader")
      .map((header) => header.textContent);

    if (isMobile) {
      expect(headers).toEqual([
        "orderList.columns.id",
        "orderList.columns.status",
        "orderList.columns.action",
      ]);
    } else {
      expect(headers).toEqual([
        "orderList.columns.id",
        "orderList.columns.customer",
        "orderList.columns.status",
        "orderList.columns.items",
        "orderList.columns.action",
      ]);
    }
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
    // 'isMobile' is a pass-through prop, but this is asserted to
    // ensure it is consistent for both variants
    expect(orderRowProps.map((props) => props.isMobile)).toEqual(
      orders.map(() => isMobile),
    );
  });
});

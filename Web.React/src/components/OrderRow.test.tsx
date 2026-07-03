import { render, screen, within } from "@testing-library/react";
import { makeOrder, makeOrderItem } from "../test/factories/orderFactory";
import { createUseOrdersMock } from "../test/factories/useOrdersFactory";
import type { Order } from "../types/order";
import { useOrders } from "../hooks/useOrders";
import OrderRow from "./OrderRow";

// Mock hooks to keep tests focused on OrderRow component
vi.mock("../hooks/useOrders");

// Mock hook handles
const updateOrderStatus = vi.fn();
const deleteOrder = vi.fn();

beforeEach(() => {
  // Reset 'useOrders' mock before each test to ensure clean state
  vi.mocked(useOrders).mockReturnValue(
    createUseOrdersMock({ updateOrderStatus, deleteOrder }),
  );
});

// Default order for testing
const defaultOrder: Order = makeOrder({
  id: 1,
  customerId: 1,
  status: "Pending",
  items: [
    makeOrderItem({ productName: "prod1" }),
    makeOrderItem({ productName: "prod2" }),
  ],
});

// Row should be rendered with table/body context
// To allow proper semantics for table-related ARIA roles
const renderRow = (order: Order = defaultOrder) => {
  return render(
    <table>
      <tbody>
        <OrderRow order={order} />
      </tbody>
    </table>,
  );
};

describe("OrderRow", () => {
  it("renders view mode with expected row count", () => {
    renderRow();

    const rows = screen.getAllByRole("row");

    // Assert that only one row is rendered
    expect(rows).toHaveLength(1);
  });

  it("renders view mode with correct cells", () => {
    renderRow();

    // Expect 4 cells in first row: ID, Customer, Status, Items
    // Skip last cell (actions) for this test
    const row = screen.getByRole("row", {
      name: new RegExp(`^Order ${defaultOrder.id}$`),
    });
    const contentCells = within(row)
      .getAllByRole("cell")
      .slice(0, -1)
      .map((cell) => cell.textContent);

    expect(contentCells).toHaveLength(4);

    // Assert cell contents match default order
    expect(contentCells).toEqual([
      defaultOrder.id.toString(),
      defaultOrder.customerId.toString(),
      defaultOrder.status,
      defaultOrder.items.map((item) => item.productName).join(", "),
    ]);
  });

  it("renders view mode with correct action buttons", () => {
    renderRow();

    const row = screen.getByRole("row", {
      name: new RegExp(`^Order ${defaultOrder.id}$`),
    });

    // Implicitly assert buttons exist
    const editButton = within(row).getByRole("button", {
      name: /edit/i,
    });
    const deleteButton = within(row).getByRole("button", {
      name: /delete/i,
    });

    // Assert buttons enabled
    expect(editButton).toBeEnabled();
    expect(deleteButton).toBeEnabled();
  });

  it("enters edit mode and updates status", async () => {
    renderRow();
  });
});

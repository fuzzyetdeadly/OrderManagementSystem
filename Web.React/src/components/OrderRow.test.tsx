import { render, screen, within } from "@testing-library/react";
import { userEvent } from "@testing-library/user-event";
import { getRow, getButton } from "../test/testUtils";
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

const user = userEvent.setup();

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
function renderRow(order: Order = defaultOrder) {
  return render(
    <table>
      <tbody>
        <OrderRow order={order} />
      </tbody>
    </table>,
  );
}

// --- Tests ---
beforeEach(() => {
  // Reset 'useOrders' mock before each test to ensure clean state
  vi.mocked(useOrders).mockReturnValue(
    createUseOrdersMock({ updateOrderStatus, deleteOrder }),
  );
});

describe("OrderRow", () => {
  it("renders view mode rows correctly", () => {
    renderRow();

    const rows = screen.getAllByRole("row");

    // Assert that only one row is rendered
    expect(rows).toHaveLength(1);
  });

  it("renders view mode cells correctly", () => {
    renderRow();

    // Expect 4 cells in first row: ID, Customer, Status, Items
    // Skip last cell (actions) for this test
    const row = getRow(`Order ${defaultOrder.id}`);
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

  it("renders view mode exclusive buttons correctly", () => {
    renderRow();

    const row = getRow(`Order ${defaultOrder.id}`);

    // Implicitly assert buttons exist
    const editButton = getButton(row, /edit/i);
    const deleteButton = getButton(row, /delete/i);

    // Assert button states as expected
    expect(editButton).toBeEnabled();
    expect(deleteButton).toBeEnabled();
  });

  it("renders edit mode exclusive elements correctly", async () => {
    renderRow();

    const row = getRow(`Order ${defaultOrder.id}`);

    // Locate edit button and enable edit mode
    const editButton = getButton(row, /edit/i);

    await user.click(editButton);

    // Assert elements exist
    const statusSelect = within(row).getByRole("combobox");
    const saveButton = getButton(row, /save/i);
    const cancelButton = getButton(row, /cancel/i);

    // Assert button states as expected
    // Asserting on pre-click reference is intentional, expect stale element
    expect(statusSelect).toBeEnabled();
    expect(saveButton).toBeDisabled();
    expect(cancelButton).toBeEnabled();
    expect(editButton).not.toBeInTheDocument();
  });

  it("reverts to view mode when edit mode is canceled", async () => {
    renderRow();

    const row = getRow(`Order ${defaultOrder.id}`);

    // Switch to edit mode
    const editButtonBefore = getButton(row, /edit/i);
    await user.click(editButtonBefore);

    // Locate edit elements
    const statusSelect = within(row).getByRole("combobox");
    const saveButton = getButton(row, /save/i);
    const cancelButton = getButton(row, /cancel/i);

    // Change status, then cancel
    await user.selectOptions(statusSelect, "Processing");
    await user.click(cancelButton);

    // Locate buttons for assertions
    const editButtonAfter = getButton(row, /edit/i);
    const deleteButton = getButton(row, /delete/i);

    // Assert that view buttons visible, and edit buttons removed
    // Asserting on pre-click reference is intentional, expect stale element
    expect(editButtonAfter).toBeEnabled();
    expect(deleteButton).toBeEnabled();
    expect(statusSelect).not.toBeInTheDocument();
    expect(saveButton).not.toBeInTheDocument();
    expect(cancelButton).not.toBeInTheDocument();

    // Assert that update hook wasn't called
    expect(updateOrderStatus).not.toHaveBeenCalled();
  });

  it("edit mode behaviors work correctly", async () => {
    renderRow();

    const row = getRow(`Order ${defaultOrder.id}`);

    // Switch to edit mode
    const editButton = getButton(row, /edit/i);
    await user.click(editButton);

    // Assert that status select exists, then change it
    const statusSelect = within(row).getByRole("combobox");
    await user.selectOptions(statusSelect, "Processing");

    // Assert that save button now enabled
    const saveButton = getButton(row, /save/i);
    expect(saveButton).toBeEnabled();

    // Save the change
    await user.click(saveButton);

    // Assert that save was processed as expected
    // Note: no check for updated order status.
    // On cache update, mutated orders are passed down from App.tsx
    expect(updateOrderStatus).toHaveBeenCalledWith(defaultOrder.id, {
      status: "Processing",
    });
  });
});

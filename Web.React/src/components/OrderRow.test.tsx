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

let user: ReturnType<typeof userEvent.setup>;

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
  // Re-initializer user per test to ensure user has fresh state
  user = userEvent.setup();

  // Reset 'useOrders' mock before each test to ensure clean state
  vi.mocked(useOrders).mockReturnValue(
    createUseOrdersMock({ updateOrderStatus, deleteOrder }),
  );
});

describe("OrderRow.ViewMode", () => {
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
      `orderRow.status.${defaultOrder.status.toLowerCase()}`,
      defaultOrder.items.map((item) => item.productName).join(", "),
    ]);
  });

  it("renders view mode buttons correctly", () => {
    renderRow();

    const row = getRow(`Order ${defaultOrder.id}`);

    // Implicitly assert buttons exist
    const editButton = getButton(row, "orderRow.buttons.edit");
    const deleteButton = getButton(row, "orderRow.buttons.delete");

    // Assert button states as expected
    expect(editButton).toBeEnabled();
    expect(deleteButton).toBeEnabled();
  });
});

describe("OrderRow.EditMode", () => {
  it("renders edit mode elements correctly", async () => {
    renderRow();

    const row = getRow(`Order ${defaultOrder.id}`);

    // Locate view buttons and enable edit mode
    const editButton = getButton(row, "orderRow.buttons.edit");
    const deleteButton = getButton(row, "orderRow.buttons.delete");

    await user.click(editButton);

    // Assert elements exist
    const statusSelect = within(row).getByRole("combobox");
    const saveButton = getButton(row, "orderRow.buttons.save");
    const cancelButton = getButton(row, "orderRow.buttons.cancel");

    // Assert button states as expected
    // Asserting on pre-click reference is intentional, expect stale element
    expect(statusSelect).toBeEnabled();
    expect(saveButton).toBeDisabled();
    expect(cancelButton).toBeEnabled();
    expect(editButton).not.toBeInTheDocument();
    expect(deleteButton).not.toBeInTheDocument();
  });

  it("reverts to view mode when edit mode is canceled", async () => {
    renderRow();

    const row = getRow(`Order ${defaultOrder.id}`);

    // Switch to edit mode
    const editButtonBefore = getButton(row, "orderRow.buttons.edit");
    await user.click(editButtonBefore);

    // Locate edit elements
    const statusSelect = within(row).getByRole("combobox");
    const saveButton = getButton(row, "orderRow.buttons.save");
    const cancelButton = getButton(row, "orderRow.buttons.cancel");

    // Change status by value, then cancel
    await user.selectOptions(statusSelect, "Processing");
    await user.click(cancelButton);

    // Locate view mode buttons
    const editButtonAfter = getButton(row, "orderRow.buttons.edit");
    const deleteButton = getButton(row, "orderRow.buttons.delete");

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

    // Switch to edit mode and change status by value
    const editButton = getButton(row, /edit/i);
    await user.click(editButton);

    const statusSelect = within(row).getByRole("combobox");
    await user.selectOptions(statusSelect, "Processing");

    // Assert that save button is enabled and save
    const saveButton = getButton(row, "orderRow.buttons.save");
    expect(saveButton).toBeEnabled();

    await user.click(saveButton);

    // Assert that save was processed as expected
    // Note: no check for updated order status.
    // On cache update, mutated orders are passed down from App.tsx
    expect(updateOrderStatus).toHaveBeenCalledWith(defaultOrder.id, {
      status: "Processing",
    });
  });

  it("edit mode displays error when save failed", async () => {
    // Mock network error
    updateOrderStatus.mockRejectedValueOnce(new Error("Network error"));

    renderRow();

    const row = getRow(`Order ${defaultOrder.id}`);

    // Switch to edit mode, change status by value and save
    const editButton = getButton(row, "orderRow.buttons.edit");
    await user.click(editButton);

    const statusSelect = within(row).getByRole("combobox");
    await user.selectOptions(statusSelect, "Processing");

    const saveButton = getButton(row, "orderRow.buttons.save");
    await user.click(saveButton);

    // Assert that error row appeared with expected error
    const errorRow = getRow("errors.errorMessage");
    expect(errorRow).toHaveTextContent("errors.failedSave");
  });
});

describe("OrderRow.DeleteMode", () => {
  it("renders delete mode buttons correctly", async () => {
    renderRow();

    const row = getRow(`Order ${defaultOrder.id}`);

    // Locate view buttons and enable delete mode
    const editButton = getButton(row, "orderRow.buttons.edit");
    const deleteButton = getButton(row, "orderRow.buttons.delete");

    await user.click(deleteButton);

    // Assert elements exist
    const confirmText = within(row).getByText("orderRow.buttons.deletePrompt");
    const confirmButton = getButton(row, "orderRow.buttons.confirmDelete");
    const cancelButton = getButton(row, "orderRow.buttons.cancel");

    // Assert button states as expected
    // Asserting on pre-click reference is intentional, expect stale element
    expect(confirmText).toBeInTheDocument();
    expect(confirmButton).toBeEnabled();
    expect(cancelButton).toBeEnabled();
    expect(editButton).not.toBeInTheDocument();
    expect(deleteButton).not.toBeInTheDocument();
  });

  it("reverts to view mode when delete mode is canceled", async () => {
    renderRow();

    const row = getRow(`Order ${defaultOrder.id}`);

    // Switch to edit mode
    const deleteButtonBefore = getButton(row, "orderRow.buttons.delete");
    await user.click(deleteButtonBefore);

    // Locate delete buttons
    const confirmButton = getButton(row, "orderRow.buttons.confirmDelete");
    const cancelButton = getButton(row, "orderRow.buttons.cancel");

    // Cancel delete mode
    await user.click(cancelButton);

    // Locate view mode buttons
    const editButton = getButton(row, "orderRow.buttons.edit");
    const deleteButtonAfter = getButton(row, "orderRow.buttons.delete");

    // Assert that view buttons visible, and edit buttons removed
    // Asserting on pre-click reference is intentional, expect stale element
    expect(editButton).toBeEnabled();
    expect(deleteButtonAfter).toBeEnabled();
    expect(confirmButton).not.toBeInTheDocument();
    expect(cancelButton).not.toBeInTheDocument();

    // Assert that update hook wasn't called
    expect(deleteOrder).not.toHaveBeenCalled();
  });

  it("delete mode behaviors work correctly", async () => {
    renderRow();

    const row = getRow(`Order ${defaultOrder.id}`);

    // Switch to delete mode
    const deleteButton = getButton(row, "orderRow.buttons.delete");
    await user.click(deleteButton);

    // Confirm deletion
    const confirmButton = getButton(row, "orderRow.buttons.confirmDelete");
    await user.click(confirmButton);

    // Assert that save was processed as expected
    // Note: no check for updated order status.
    // On cache update, mutated orders are passed down from App.tsx
    expect(deleteOrder).toHaveBeenCalledWith(defaultOrder.id);
  });

  it("delete mode displays error when delete failed", async () => {
    // Mock network error
    deleteOrder.mockRejectedValueOnce(new Error("Network error"));

    renderRow();

    const row = getRow(`Order ${defaultOrder.id}`);

    // Switch to edit mode, change status and save
    const deleteButton = getButton(row, "orderRow.buttons.delete");
    await user.click(deleteButton);

    const confirmButton = getButton(row, "orderRow.buttons.confirmDelete");
    await user.click(confirmButton);

    // Assert that error row appeared with expected error
    const errorRow = getRow("errors.errorMessage");
    expect(errorRow).toHaveTextContent("errors.failedDelete");
  });
});

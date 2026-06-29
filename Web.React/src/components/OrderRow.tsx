import { useState } from "react";
import { useOrders } from "../hooks/useOrders";
import { ORDER_STATUSES } from "../types/order";
import type {
  OrderStatus,
  Order,
  UpdateOrderStatusPayload,
} from "../types/order";

// Prefer union type over enum, as it is more idiomatic for TS (26/06/dd)
type RowMode = "view" | "edit" | "confirmDelete";

type OrderRowProps = {
  order: Order;
};

export default function OrderRow({ order }: OrderRowProps) {
  const { updateOrderStatus, deleteOrder } = useOrders();

  const [mode, setMode] = useState<RowMode>("view");
  const [selectedStatus, setSelectedStatus] = useState<OrderStatus>(
    order.status,
  );
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const hasChanges = selectedStatus !== order.status;

  // Button functions
  const handleSave = async () => {
    setLoading(true);
    setError("");

    try {
      // Construct payload with selected status
      const payload: UpdateOrderStatusPayload = { status: selectedStatus };

      await updateOrderStatus(order.id, payload);
      setMode("view");
    } catch {
      setError("Failed to save.");
    } finally {
      setLoading(false);
    }
  };

  const handleCancel = () => {
    // Revert selected status
    setSelectedStatus(order.status);
    setError("");
    setMode("view");
  };

  const handleDelete = async () => {
    setLoading(true);
    setError("");

    try {
      await deleteOrder(order.id);
    } catch {
      setError("Failed to delete.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <>
      <tr className="order-row">
        <td>{order.id}</td>
        <td>{order.customerId}</td>
        <td>
          {mode == "edit" ? (
            <select
              className="status-select"
              value={selectedStatus}
              onChange={(e) => setSelectedStatus(e.target.value as OrderStatus)}
            >
              {ORDER_STATUSES.map((status) => (
                <option key={status} value={status}>
                  {status}
                </option>
              ))}
            </select>
          ) : (
            order.status
          )}
        </td>
        <td>{order.items.map((i) => i.productName).join(", ")}</td>
        <td>
          <div className="row-actions">
            {/*Require 'div' wrapper. 'td' line-height doesn't work correctly with flex*/}
            {mode === "view" && (
              <>
                <button
                  className="btn-icon"
                  onClick={() => setMode("edit")}
                  title="Edit"
                >
                  ✏️
                </button>
                <button
                  className="btn-icon"
                  onClick={() => setMode("confirmDelete")}
                  title="Delete"
                >
                  🗑️
                </button>
              </>
            )}
            {mode === "edit" && (
              <>
                <button
                  className="btn-icon"
                  onClick={handleSave}
                  disabled={!hasChanges || loading}
                  title="Save"
                >
                  ✔️
                </button>
                <button
                  className="btn-icon"
                  onClick={handleCancel}
                  disabled={loading}
                  title="Cancel"
                >
                  ❌
                </button>
              </>
            )}
            {mode === "confirmDelete" && (
              <>
                <span className="delete-prompt">Delete?</span>
                <button
                  className="btn-icon"
                  onClick={handleDelete}
                  disabled={loading}
                  title="Confirm delete"
                >
                  ✔️
                </button>
                <button
                  className="btn-icon"
                  onClick={() => setMode("view")}
                  disabled={loading}
                  title="Cancel"
                >
                  ❌
                </button>
              </>
            )}
          </div>
        </td>
      </tr>
      {error && (
        <tr>
          <td colSpan={5} className="row-error">
            {error}
          </td>
        </tr>
      )}
    </>
  );
}

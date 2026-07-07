import { useTranslation } from "react-i18next";
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
  const { t } = useTranslation();
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
      setError(t("errors.failedSave"));
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
      setError(t("errors.failedDelete"));
    } finally {
      setLoading(false);
    }
  };

  return (
    <>
      <tr className="order-row" aria-label={`Order ${order.id}`}>
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
                  {t(`orderRow.status.${status.toLowerCase()}`)}
                </option>
              ))}
            </select>
          ) : (
            t(`orderRow.status.${order.status.toLowerCase()}`)
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
                  title={t("orderRow.buttons.edit")}
                  aria-label={t("orderRow.buttons.edit")}
                >
                  ✏️
                </button>
                <button
                  className="btn-icon"
                  onClick={() => setMode("confirmDelete")}
                  title={t("orderRow.buttons.delete")}
                  aria-label={t("orderRow.buttons.delete")}
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
                  title={t("orderRow.buttons.save")}
                  aria-label={t("orderRow.buttons.save")}
                >
                  ✔️
                </button>
                <button
                  className="btn-icon"
                  onClick={handleCancel}
                  disabled={loading}
                  title={t("orderRow.buttons.cancel")}
                  aria-label={t("orderRow.buttons.cancel")}
                >
                  ❌
                </button>
              </>
            )}
            {mode === "confirmDelete" && (
              <>
                <span className="delete-prompt">
                  {t("orderRow.buttons.deletePrompt")}
                </span>
                <button
                  className="btn-icon"
                  onClick={handleDelete}
                  disabled={loading}
                  title={t("orderRow.buttons.confirmDelete")}
                  aria-label={t("orderRow.buttons.confirmDelete")}
                >
                  ✔️
                </button>
                <button
                  className="btn-icon"
                  onClick={() => setMode("view")}
                  disabled={loading}
                  title={t("orderRow.buttons.cancel")}
                  aria-label={t("orderRow.buttons.cancel")}
                >
                  ❌
                </button>
              </>
            )}
          </div>
        </td>
      </tr>
      {error && (
        <tr aria-label={t("errors.errorMessage")}>
          <td colSpan={5} className="row-error">
            {error}
          </td>
        </tr>
      )}
    </>
  );
}

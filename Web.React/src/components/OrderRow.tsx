import { useTranslation } from "react-i18next";
import { useState } from "react";
import TableRow from "@mui/material/TableRow";
import TableCell from "@mui/material/TableCell";
import Select from "@mui/material/Select";
import MenuItem from "@mui/material/MenuItem";
import IconButton from "@mui/material/IconButton";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import Alert from "@mui/material/Alert";
import EditIcon from "@mui/icons-material/Edit";
import DeleteIcon from "@mui/icons-material/Delete";
import CheckIcon from "@mui/icons-material/Check";
import CloseIcon from "@mui/icons-material/Close";
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
  isMobile?: boolean;
};

export default function OrderRow({ order, isMobile }: OrderRowProps) {
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
      <TableRow aria-label={`Order ${order.id}`}>
        <TableCell>{order.id}</TableCell>
        {!isMobile && <TableCell>{order.customerId}</TableCell>}
        <TableCell>
          {mode == "edit" ? (
            <Select
              size="small"
              value={selectedStatus}
              onChange={(e) => setSelectedStatus(e.target.value as OrderStatus)}
            >
              {ORDER_STATUSES.map((status) => (
                <MenuItem key={status} value={status}>
                  {t(`orderRow.status.${status.toLowerCase()}`)}
                </MenuItem>
              ))}
            </Select>
          ) : (
            t(`orderRow.status.${order.status.toLowerCase()}`)
          )}
        </TableCell>
        {!isMobile && (
          <TableCell>
            {order.items.map((i) => i.productName).join(", ")}
          </TableCell>
        )}
        <TableCell>
          <Stack
            direction="row"
            sx={{ justifyContent: "flex-end", alignItems: "center", gap: 0.5 }}
          >
            {mode === "view" && (
              <>
                <IconButton
                  size="small"
                  onClick={() => setMode("edit")}
                  title={t("orderRow.buttons.edit")}
                  aria-label={t("orderRow.buttons.edit")}
                >
                  <EditIcon fontSize="small" />
                </IconButton>
                <IconButton
                  size="small"
                  color="error"
                  onClick={() => setMode("confirmDelete")}
                  title={t("orderRow.buttons.delete")}
                  aria-label={t("orderRow.buttons.delete")}
                >
                  <DeleteIcon fontSize="small" />
                </IconButton>
              </>
            )}
            {mode === "edit" && (
              <>
                <IconButton
                  size="small"
                  color="success"
                  onClick={handleSave}
                  disabled={!hasChanges || loading}
                  title={t("orderRow.buttons.save")}
                  aria-label={t("orderRow.buttons.save")}
                >
                  <CheckIcon fontSize="small" />
                </IconButton>
                <IconButton
                  size="small"
                  onClick={handleCancel}
                  disabled={loading}
                  title={t("orderRow.buttons.cancel")}
                  aria-label={t("orderRow.buttons.cancel")}
                >
                  <CloseIcon fontSize="small" />
                </IconButton>
              </>
            )}
            {mode === "confirmDelete" && (
              <>
                <Typography variant="caption" sx={{ mr: 0.5 }}>
                  {t("orderRow.buttons.deletePrompt")}
                </Typography>
                <IconButton
                  size="small"
                  color="error"
                  onClick={handleDelete}
                  disabled={loading}
                  title={t("orderRow.buttons.confirmDelete")}
                  aria-label={t("orderRow.buttons.confirmDelete")}
                >
                  <CheckIcon fontSize="small" />
                </IconButton>
                <IconButton
                  size="small"
                  onClick={() => setMode("view")}
                  disabled={loading}
                  title={t("orderRow.buttons.cancel")}
                  aria-label={t("orderRow.buttons.cancel")}
                >
                  <CloseIcon fontSize="small" />
                </IconButton>
              </>
            )}
          </Stack>
        </TableCell>
      </TableRow>
      {error && (
        <TableRow aria-label={t("errors.errorMessage")}>
          <TableCell colSpan={isMobile ? 3 : 5} sx={{ py: 0.5 }}>
            <Alert variant="outlined" severity="error">
              {error}
            </Alert>
          </TableCell>
        </TableRow>
      )}
    </>
  );

  /*
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
            {// Require 'div' wrapper. 'td' line-height doesn't work correctly with flex}
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
  */
}

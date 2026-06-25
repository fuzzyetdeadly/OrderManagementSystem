import { useState } from "react";
import type { Order } from "../types/order";
import { updateOrderStatus, deleteOrder } from "../api/orders";

const ORDER_STATUSES = ["Pending", "Processing", "Completed", "Cancelled"];

// Prefer union type over enum, as it is more idiomatic for TS (26/06/dd)
type RowMode = "view" | "edit" | "confirmDelete";

type OrderRowProps = {
	order: Order;
	onUpdated: (updated: Order) => void;
	onDeleted: (id: number) => void;
}

export default function OrderRow({ order, onUpdated, onDeleted }: OrderRowProps) {
	const [mode, setMode] = useState<RowMode>("view");
	const [selectedStatus, setSelectedStatus] = useState(order.status);
	const [loading, setLoading] = useState(false);
	const [error, setError] = useState("");
	
	const hasChanges = selectedStatus !== order.status;
	
	// Button functions
	const handleSave = async () => {
		setLoading(true);
		setError("");
		
		try {
			// Construct payload with selected status
			const payload: UpdateOrderStatusPayload = {
				status: selectedStatus
			}
			
			const updated = await updateOrderStatus(order.id, payload);
			onUpdated(updated);
			setMode("view");
		} catch {
			setError("Failed to save.");
		} finally {
			setLoading(false);
		}
	}
	
	const handleCancel = () => {
		// Revert selected status
		setSelectedStatus(order.status);
		setError("");
		setMode("view");
	}
	
	const handleDelete = async () => {
		setLoading(true);
		setError("");
		
		try {
			await deleteOrder(order.id);
			onDeleted(order.id);
		} catch {
			setError("Failed to delete.");
		} finally {
			setLoading(false);
		}
	}
	
	return (
		<>
			<tr>
				<td>{order.id}</td>
				<td>{order.customerId}</td>
				<td>
					{mode == "edit" ? (
						<select
							className="status-select"
							value={selectedStatus}
							onChange={(e) => setSelectedStatus(e.target.value)}
						>
							{ORDER_STATUSES.map((status) => (
								<option key={status} value={status}>{status}</option>
							))}
						</select>
					) : (
						order.status
					)}
				</td>
				<td>{order.items.map((i) => i.productName).join(", ")}</td>
				<td className="row-actions">
					{mode === "view" && (
						<button className="btn-icon" onClick={() => setMode("edit")}>✏️</button>
					)}
					{mode === "edit" && (
						<>
							<button 
								className="btn-icon" 
								onClick={handleSave}
								disabled={!hasChanges || loading}
								title="Save"
							>✔️</button>
							<button 
								className="btn-icon"
								onClick={handleCancel}
								disabled={loading}
								title="Cancel"
							>❌</button>
							<button 
								className="btn-icon btn-delete"
								onClick={() => setMode("confirmDelete")}
								disabled={loading}
								title="Delete"
							>🗑️</button>
						</>
					)}
					{mode === "confirmDelete" && (
						<>
							<button 
								className="btn-icon" 
								onClick={handleDelete}
								disabled={loading}
								title="Confirm delete"
							>✔️</button>
							<button 
								className="btn-icon"
								onClick={() => setMode("edit")}
								disabled={loading}
								title="Go back"
							>↩️</button>
						</>
					)}
				</td>
			</tr>
			{error && (
				<tr>
					<td colSpan={5} className="row-error">{error}</td>
				</tr>
			)}
		</>
	);
}
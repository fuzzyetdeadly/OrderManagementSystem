import { useState } from "react";
import { createOrder } from "../api/orders";
import type { CreateOrderPayload } from "../types/order";

interface AddOrderProps {
	onCreated: () => void;
}

export default function AddOrderForm({ onCreated }: AddOrderProps) {
	// Set default values
	const [customerId, setCustomerId] = useState(1);
	const [productName, setProductName] = useState("");
	const [quantity, setQuantity] = useState(1);
	const [unitPrice, setUnitPrice] = useState(0);
	
	const handleSubmit = async () => {
		// Post the order
		const payload: CreateOrderPayload = {
			customerId,
			items: [{ productName, quantity, unitPrice }],
		};
		await createOrder(payload);
		
		// Invoke 'onCreated' callback
		onCreated();
	};
	
	return (
		<div>
			<input type="number" value={customerId} onChange={(e) => setCustomerId(+e.target.value)} placeholder="Customer ID" />
			<input value={productName} onChange={(e) => setProductName(e.target.value)} placeholder="Product name" />
			<input type="number" value={quantity} onChange={(e) => setQuantity(+e.target.value)} placeholder="Quantity" />
			<input type="number" value={unitPrice} onChange={(e) => setUnitPrice(+e.target.value)} placeholder="Unit price" />
			<button onClick={handleSubmit}>Add Order</button>
		</div>
	);
}
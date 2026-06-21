import { useState } from "react";
import axios from "axios";
import { createOrder } from "../api/orders";
import type { CreateOrderPayload } from "../types/order";
import type { ValidationProblemDetails } from "../types/common";

interface AddOrderProps {
	onCreated: () => void;
}

export default function AddOrderForm({ onCreated }: AddOrderProps) {
	// Set default values (used for initial render)
	const [customerId, setCustomerId] = useState(1);
	const [productName, setProductName] = useState("");
	const [quantity, setQuantity] = useState(1);
	const [unitPrice, setUnitPrice] = useState(0);
	const [errors, setErrors] = useState<string[]>([]);
	
	const handleSubmit = async () => {
		// Reset errors
		setErrors([]);
		
		// Post the order
		const payload: CreateOrderPayload = {
			customerId,
			items: [{ productName, quantity, unitPrice }],
		};
		
		try {
			await createOrder(payload);

			// Invoke 'onCreated' callback
			onCreated();
		} catch(err) {
			if(axios.isAxiosError<ValidationProblemDetails>(err) && err.response?.data?.errors) {
				setErrors(Object.values(err.response.data.errors).flat());
			} else {
				setErrors(["Something went wrong. Please try again"]);
			}
		}
	};
	
	return (
		<div>
			<input type="number" value={customerId} onChange={(e) => setCustomerId(+e.target.value)} placeholder="Customer ID" />
			<input value={productName} onChange={(e) => setProductName(e.target.value)} placeholder="Product name" />
			<input type="number" value={quantity} onChange={(e) => setQuantity(+e.target.value)} placeholder="Quantity" />
			<input type="number" value={unitPrice} onChange={(e) => setUnitPrice(+e.target.value)} placeholder="Unit price" />
			<button onClick={handleSubmit}>Add Order</button>
			
			{errors.length > 0 && (
				<ul className="form-errors">
					{errors.map((msg, i) => (
						<li key={i}>{msg}</li>
					))}
				</ul>
			)}
		</div>
	);
}
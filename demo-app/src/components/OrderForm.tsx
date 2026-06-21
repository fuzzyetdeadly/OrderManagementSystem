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
			// Temporary error handling until proper front end input validation added
			if(axios.isAxiosError<ValidationProblemDetails>(err) && err.response?.data?.errors) {
				setErrors(Object.values(err.response.data.errors).flat());
			} else {
				setErrors(["Something went wrong. Please try again"]);
			}
		}
	};
	
	return (
		<div className="order-form">
			<div className="form-field">
				<label htmlFor="customerId">Customer ID</label>
				<input 
					id="customerId" type="number" value={customerId} 
					onChange={(e) => setCustomerId(+e.target.value)} />
			</div>
			
			<div className="form-field">
				<label htmlFor="productName">Product name</label>
				<input 
					id="productName" value={productName} placeholder="e.g. Potato"
					onChange={(e) => setProductName(e.target.value)} />
			</div>
			
			<div className="form-field">
				<label htmlFor="quantity">Quantity</label>
				<input 
					id="quantity" type="number" value={quantity}
					onChange={(e) => setQuantity(+e.target.value)} />
			</div>
			
			<div className="form-field">
				<label htmlFor="unitPrice">Unit price</label>
				<input 
					id="unitPrice" type="number" value={unitPrice}
					onChange={(e) => setUnitPrice(+e.target.value)} />
			</div>
			
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
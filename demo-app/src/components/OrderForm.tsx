import { useRef, useState } from "react";
import axios from "axios";
import { createOrder } from "../api/orders";
import type { CreateOrderPayload } from "../types/order";
import type { ValidationProblemDetails } from "../types/common";

interface AddOrderProps {
	onCreated: () => void;
}

const PRODUCT_OPTIONS = ["Carrot", "Eggplant", "Garlic", "Potato", "Spinach"];
const UNSELECTED = "";

export default function OrderForm({ onCreated }: AddOrderProps) {
	// Set default values (used for initial render)
	const [customerId, setCustomerId] = useState(1);
	const [productName, setProductName] = useState(UNSELECTED);
	const [quantity, setQuantity] = useState(1);
	const [unitPrice, setUnitPrice] = useState(0.01);
	const [errors, setErrors] = useState<string[]>([]);
	const [productInvalid, setProductInvalid] = useState(false);
	
	// Reference to productName element
	const productNameRef = useRef<HTMLSelectELement>(null);
	
	const handleSubmit = async () => {
		// Reset errors
		setErrors([]);
		
		// Validate product selection		
		if(productName === UNSELECTED) {
			setProductInvalid(true);
			productNameRef.current?.focus();
			setErrors(["Please select a product"]);
			return;
		}
		
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
					id="customerId" type="number" min={1} value={customerId} 
					onChange={(e) => setCustomerId(+e.target.value)} />
			</div>
			
			<div className="form-field">
				<label htmlFor="productName">Product name*</label>
				<select 
					id="productName" ref={productNameRef} value={productName}
					className={productInvalid ? "input-invalid" : ""}
					onChange={(e) => {
						setProductName(e.target.value);
						if(e.target.value !== UNSELECTED)
						{
							setProductInvalid(false);
						}	
					}}>
					<option value={UNSELECTED}>Select</option>
					{PRODUCT_OPTIONS.map(option => (
						<option key={option} value={option}>{option}</option>		
					))}
				</select>
			</div>
			
			<div className="form-field">
				<label htmlFor="quantity">Quantity</label>
				<input 
					id="quantity" type="number" min={1} value={quantity}
					onChange={(e) => setQuantity(+e.target.value)} />
			</div>
			
			<div className="form-field">
				<label htmlFor="unitPrice">Unit price</label>
				<input 
					id="unitPrice" type="number" min={0.01} step="0.01" value={unitPrice}
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
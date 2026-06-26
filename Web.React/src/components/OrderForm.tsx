import { useForm } from "react-hook-form";
import axios from "axios";
import { useOrders } from "../hooks/useOrders";
import type { CreateOrderPayload } from "../types/order";
import type { ValidationProblemDetails } from "../types/common";

// Data shape for RHF useForm
type OrderFormValues = {
	customerId: number;
	productName: string;
	quantity: number;
	unitPrice: number;
}

const PRODUCT_OPTIONS = ["Carrot", "Eggplant", "Garlic", "Potato", "Spinach"];
const UNSELECTED = "";
const UNSELECTED_ERROR = "Please select a product";
const UNKNOWN_ERROR = "Something went wrong. Please try again.";

// Note: no props because queries handled by 'useOrders' hook
export default function OrderForm() {
	const { createOrder } = useOrders();
	
	// UseForm returns functions/states that can be used
	// For server-side errors from the catch block, use setError
	// to update the internal state of RHF.
	const { register, handleSubmit, setError, clearErrors, formState: { errors }} = useForm<OrderFormValues>({
		mode: "onChange",
		defaultValues: {
				customerId: 1,
				productName: UNSELECTED,
				quantity: 1,
				unitPrice: 0.01,
		}
	});
	
	// RHF's handleSubmit will wrap this method,
	// injecting validated values and blocking invalid submissions
	const onSubmit = async (data: OrderFormValues) => {
		// Clear all errors on "root" node
		clearErrors("root");
		
		const payload: CreateOrderPayload = {
			customerId: data.customerId,
			items: [{ 
				productName: data.productName, 
				quantity: data.quantity, 
				unitPrice: data.unitPrice }],
		};
		
		try {
			// Create order, then invoke callback
			await createOrder(payload);
		} catch(err) {
			// 400 status usually returns *.errors, while 404 returns *.detail
			if(axios.isAxiosError<ValidationProblemDetails>(err) && err.response?.data) {
				// Cannot use 'data', because it is used by onSubmit's anon function
				const responseData = err.response.data;
				if(responseData.errors) {
					Object.values(responseData.errors).flat().forEach((msg, i) => {
						// Note: if there are multiple errors, use only the last
						// Unlikely to reach here unless form isn't validating everything it should.
						setError("root", { message: msg });
					});
				} else if (responseData.detail) {
					setError("root", { message: responseData.detail });
				} else {
					setError("root", { message: UNKNOWN_ERROR });
				}
			}
		}
	};
	
	return (
		<div className="order-form">
			<div className="form-field">
				<label htmlFor="customerId">Customer ID</label>
				<input 
					id="customerId" type="number" min={1}
					{...register("customerId", { valueAsNumber: true, min: 1 })}
					onChange={() => clearErrors("root")} />
			</div>
			
			<div className="form-field">
				<label htmlFor="productName">Product name*</label>
				<select 
					id="productName"
					{...register("productName", { validate: v => v !== UNSELECTED || UNSELECTED_ERROR })} >
					<option value={UNSELECTED}>Select</option>
					{PRODUCT_OPTIONS.map(option => (
						<option key={option} value={option}>{option}</option>		
					))}
				</select>
			</div>
			
			<div className="form-field">
				<label htmlFor="quantity">Quantity</label>
				<input 
					id="quantity" type="number" min={1}
					{...register("quantity", { 
							valueAsNumber: true,
							required: "Quantity is required",
							min: { value: 1, message: "Quantity must be at least 1"}
						})} />
			</div>
			
			<div className="form-field">
				<label htmlFor="unitPrice">Unit price</label>
				<input 
					id="unitPrice" type="number" min={0.01} step="0.01"
					{...register("unitPrice", { 
							valueAsNumber: true,
							required: "Unit price is required",
							min: { value: 0.01, message: "Unit price must be at least 0.01"}
						})}					/>
			</div>
			
			<button onClick={handleSubmit(onSubmit)}>Add Order</button>
			
			{Object.values(errors).length > 0 && (
				<ul className="form-errors">
					{Object.values(errors).map((err, i) => (
						<li key={i}>{err?.message}</li>
					))}
				</ul>
			)}
		</div>
	);
}
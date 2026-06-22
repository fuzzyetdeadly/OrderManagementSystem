export type Order = {
	id: number;
	status: string;
	created: string;
	customerId: number;
	items: OrderItem[];
}

export type OrderItem = {
	productName: string;
	quantity: number;
	unitPrice: number;
}

export type CreateOrderPayload = {
	customerId: number;
	items: OrderItem[];
}
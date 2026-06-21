export interface Order {
	id: number;
	status: string;
	created: string;
	customerId: number;
	items: OrderItem[];
}

export interface OrderItem {
	productName: string;
	quantity: number;
	unitPrice: number;
}

export interface CreateOrderPayload {
	customerId: number;
	items: OrderItem[];
}
import type { Order } from "../types/order";

interface OrderRowProps {
	order: Order;
}

function OrderRow({ order }: OrderRowProps) {
	return (
		<tr>
			<td>{order.id}</td>
			<td>{order.customerId}</td>
			<td>{order.status}</td>
			<td>{order.items.map((i) => i.productName).join(", ")}</td>						
		</tr>
	);
}

interface OrderListProps {
	orders: Order[];
}

export default function OrderList({ orders }: OrderListProps) {
	return (
		<div className="order-list">
			<table>
				<thead>
					<tr>
						<th>ID</th><th>Customer</th><th>Status</th><th>Items</th>
					</tr>
				</thead>
				<tbody>
					{orders.map(order => (
						<OrderRow key={order.id} order={order} />
					))}
				</tbody>
			</table>
		</div>
	);
}

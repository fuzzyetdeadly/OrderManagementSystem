import type { Order } from "../types/order";
import OrderRow from "./OrderRow";

type OrderListProps = {
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

import type { Order } from "../types/order";
import OrderRow from "./OrderRow";

type OrderListProps = {
	orders: Order[];
}

export default function OrderList({ orders }: OrderListProps) {
	return (
		<div className="order-list">
			<table>
				{/*colgroup used to control column widths*/}
				<colgroup>
					<col style={{ width: "5%"}} />
					<col style={{ width: "15%"}} />
					<col style={{ width: "20%"}} />
					<col style={{ width: "40%"}} />
					<col style={{ width: "20%"}} />
				</colgroup>
				<thead>
					<tr>
						<th>ID</th>
						<th>Customer</th>
						<th>Status</th>
						<th>Items</th>
						<th>Actions</th>
					</tr>
				</thead>
				<tbody>
					{orders.map(order =>
						<OrderRow key={order.id} order={order} />
					)}
				</tbody>
			</table>
		</div>
	);
}

import type { Order } from "../types/order";

interface Props {
	orders: Order[];
}

export default function OrderList({ orders }: Props) {
	return (
		<table>
			<thead>
				<tr>
					<th>ID</th><th>Customer</th><th>Status</th><th>Items</th>
				</tr>
			</thead>
			<tbody>
				{orders.map((o) => (
					<tr key={o.id}>
						<td>{o.id}</td>
						<td>{o.customerId}</td>
						<td>{o.status}</td>
						<td>{o.items.map((i) => i.productName).join(", ")}</td>						
					</tr>
				))}
			</tbody>
		</table>
	);
}
import type { Order } from "../types/order";

type OrderRowProps = {
	order: Order;
}

export default function OrderRow({ order }: OrderRowProps) {
	return (
		<tr>
			<td>{order.id}</td>
			<td>{order.customerId}</td>
			<td>{order.status}</td>
			<td>{order.items.map((i) => i.productName).join(", ")}</td>						
		</tr>
	);
}
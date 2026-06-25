import { useEffect, useState } from "react";
import "./App.css";

import { getOrders } from "./api/orders";
import type { Order } from "./types/order";
import OrderForm from "./components/OrderForm";
import OrderList from "./components/OrderList";

function App() {
	const [orders, setOrders] = useState<Order[]>([]);
	
	// Note: default pagination params is '{}' for 'getOrders'
	const fetchOrders = async () => setOrders(await getOrders());
	
	// For updates replace the updated order with the new one
	const handleUpdated = (updated: Order) =>
		setOrders((prev) => prev.map((order) => (order.id === updated.id) ? updated : order));
	
	// For delete filter out deleted order
	const handleDeleted = (id: number) =>
		setOrders((prev) => prev.filter((order) => order.id !== id));
	
	useEffect(() => {
		fetchOrders();
	}, []);

  return (
    <>
		  <h1>Order Management</h1>
			<OrderForm onCreated={fetchOrders} />
			<OrderList 
				orders={orders}
				onUpdated={handleUpdated}
				onDeleted={handleDeleted} />
    </>
  )
}

export default App

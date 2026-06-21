import { useEffect, useState } from "react";
import "./App.css";

import { getOrders } from "./api/orders";
import type { Order } from "./types/order";
import AddOrderForm from "./components/AddOrderForm";
import OrderList from "./components/OrderList";

function App() {
	const [orders, setOrders] = useState<Order[]>([]);
	
	// Note: default pagination params is '{}' for 'getOrders'
	const fetchOrders = async () => setOrders(await getOrders());
	
	useEffect(() => {
		fetchOrders();
	}, []);

  return (
    <>
		  <h1>Order Management</h1>
			<AddOrderForm onCreated={fetchOrders} />
			<OrderList orders={orders} />
    </>
  )
}

export default App

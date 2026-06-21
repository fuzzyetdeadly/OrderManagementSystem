import { useEffect, useState } from "react";
import "./App.css";

import { getOrders } from "./api/orders";
import type { Order } from "./types/order";

import OrderList from "./components/OrderList";

function App() {
	const [orders, setOrders] = useState<Order[]>([]);
	
	const fetchOrders = async () => setOrders(await getOrders());
	
	useEffect(() => {
		fetchOrders();
	}, []);

  return (
    <>
		  <h1>Order Management</h1>
			<OrderList orders={orders} />
    </>
  )
}

export default App

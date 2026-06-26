import { useOrders } from "./hooks/useOrders";
import OrderForm from "./components/OrderForm";
import OrderList from "./components/OrderList";
import "./App.css";

function App() {
	// Note: decided to keep orders passed as prop to OrderList
	// May move into OrderList in future
	// Orders is destructured from ordersQuery and guarded with []
	const { ordersQuery } = useOrders();
	const { data: orders = []} = ordersQuery;
	
  return (
    <>
		  <h1>Order Management</h1>
			<OrderForm />
			<OrderList orders={orders} />
    </>
  )
}

export default App

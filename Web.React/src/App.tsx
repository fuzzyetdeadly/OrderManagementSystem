import { useTranslation } from "react-i18next";
import { useOrders } from "./hooks/useOrders";
import type { Order } from "./types/order";
import OrderForm from "./components/OrderForm";
import OrderList from "./components/OrderList";
import "./App.css";

function App() {
  const { t } = useTranslation();

  // Note: decided to keep orders passed as prop to OrderList
  // May move into OrderList in future
  // Orders is destructured from ordersQuery and guarded with []
  // It is inferred to have type Order[] by TS.
  const { ordersQuery } = useOrders();
  const orders: Order[] = ordersQuery.data ?? [];

  return (
    <>
      <h1>{t("app.title")}</h1>
      <OrderForm />
      <OrderList orders={orders} />
    </>
  );
}

export default App;

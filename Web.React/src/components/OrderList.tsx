import { useTranslation } from "react-i18next";
import type { Order } from "../types/order";
import OrderRow from "./OrderRow";

type OrderListProps = {
  orders: Order[];
};

export default function OrderList({ orders }: OrderListProps) {
  const { t } = useTranslation();

  return (
    <div className="order-list">
      <table>
        {/*colgroup used to control column widths*/}
        <colgroup>
          <col style={{ width: "5%" }} />
          <col style={{ width: "15%" }} />
          <col style={{ width: "20%" }} />
          <col style={{ width: "40%" }} />
          <col style={{ width: "20%" }} />
        </colgroup>
        <thead>
          <tr>
            <th>{t("orderList.columns.id")}</th>
            <th>{t("orderList.columns.customer")}</th>
            <th>{t("orderList.columns.status")}</th>
            <th>{t("orderList.columns.items")}</th>
            <th>{t("orderList.columns.action")}</th>
          </tr>
        </thead>
        <tbody>
          {orders.map((order) => (
            <OrderRow key={order.id} order={order} />
          ))}
        </tbody>
      </table>
    </div>
  );
}

import { useTranslation } from "react-i18next";
import TableContainer from "@mui/material/TableContainer";
import Table from "@mui/material/Table";
import TableHead from "@mui/material/TableHead";
import TableBody from "@mui/material/TableBody";
import TableRow from "@mui/material/TableRow";
import TableCell from "@mui/material/TableCell";
import Paper from "@mui/material/Paper";
import { useTheme } from "@mui/material/styles";
import useMediaQuery from "@mui/material/useMediaQuery";
import type { Order } from "../types/order";
import OrderRow from "./OrderRow";

type OrderListProps = {
  orders: Order[];
};

export default function OrderList({ orders }: OrderListProps) {
  const { t } = useTranslation();
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down("sm"));

  return (
    <TableContainer component={Paper} variant="outlined">
      <Table
        size={isMobile ? "small" : "medium"}
        sx={{ tableLayout: "fixed", width: 1 }}
      >
        <colgroup>
          {isMobile ? (
            <>
              <col style={{ width: "10%" }} />
              <col style={{ width: "45%" }} />
              <col style={{ width: "45%" }} />
            </>
          ) : (
            <>
              <col style={{ width: "5%" }} />
              <col style={{ width: "15%" }} />
              <col style={{ width: "25%" }} />
              <col style={{ width: "35%" }} />
              <col style={{ width: "30%" }} />
            </>
          )}
        </colgroup>
        <TableHead>
          <TableRow>
            <TableCell>{t("orderList.columns.id")}</TableCell>
            {!isMobile && (
              <TableCell>{t("orderList.columns.customer")}</TableCell>
            )}
            <TableCell>{t("orderList.columns.status")}</TableCell>
            {!isMobile && <TableCell>{t("orderList.columns.items")}</TableCell>}
            <TableCell>{t("orderList.columns.action")}</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {orders.map((order) => (
            <OrderRow key={order.id} order={order} isMobile={isMobile} />
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  );
}

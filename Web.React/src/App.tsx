import { useTranslation } from "react-i18next";
import { useColorScheme } from "@mui/material/styles";
// import Box from "@mui/material/Box";
import Container from "@mui/material/Container";
import Stack from "@mui/material/Stack";
import Switch from "@mui/material/Switch";
import Typography from "@mui/material/Typography";
import DarkModeIcon from "@mui/icons-material/DarkMode";
import LightModeIcon from "@mui/icons-material/LightMode";
import { useOrders } from "./hooks/useOrders";
import type { Order } from "./types/order";
import OrderForm from "./components/OrderForm";
import OrderList from "./components/OrderList";
import "./App.css";

function App() {
  const { t } = useTranslation();
  const { mode, setMode } = useColorScheme();

  const isDark = mode === "dark";
  const toggleMode = () => setMode(isDark ? "light" : "dark");

  // Note: decided to keep orders passed as prop to OrderList
  // May move into OrderList in future
  // Orders is destructured from ordersQuery and guarded with []
  // It is inferred to have type Order[] by TS.
  const { ordersQuery } = useOrders();
  const orders: Order[] = ordersQuery.data ?? [];

  return (
    <Container maxWidth="lg" sx={{ py: { xs: 2, sm: 4 } }}>
      <Stack spacing={{ xs: 2, sm: 3 }}>
        <Stack
          direction="row"
          sx={{
            justifyContent: "space-between",
          }}
        >
          <Typography variant="h4" component="h1">
            {t("app.title")}
          </Typography>
          <Stack
            direction="row"
            spacing={0.5}
            sx={{
              alignItems: "center",
            }}
          >
            <LightModeIcon fontSize="small" />
            <Switch
              checked={isDark}
              onChange={toggleMode}
              aria-label={t("app.toggleTheme")}
            />
            <DarkModeIcon fontSize="small" />
          </Stack>
        </Stack>
        <OrderForm />
        <OrderList orders={orders} />
      </Stack>
    </Container>
  );
}

export default App;

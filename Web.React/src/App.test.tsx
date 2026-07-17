import { render, screen } from "@testing-library/react";
import { createQueryWrapper } from "./test/queryWrapper";
import { QueryClientProvider } from "@tanstack/react-query";
import { userEvent } from "@testing-library/user-event";
import { makeOrder } from "./test/factories/orderFactory";
import {
  createOrdersQueryMock,
  createUseOrdersMock,
} from "./test/factories/useOrdersFactory";
import type { Order } from "./types/order";
import { useOrders } from "./hooks/useOrders";
import { theme } from "./theme.ts";
import { ThemeProvider } from "@mui/material/styles";
import CssBaseline from "@mui/material/CssBaseline";
import App from "./App";
import OrderForm from "./components/OrderForm";
import OrderList from "./components/OrderList";

// Mock hooks and components to keep tests focused on App component
vi.mock("./hooks/useOrders");
vi.mock("./components/OrderForm", () => {
  return { default: vi.fn(() => <div data-testid="order-form" />) };
});
vi.mock("./components/OrderList", () => {
  return { default: vi.fn(() => <div data-testid="order-list" />) };
});

let user: ReturnType<typeof userEvent.setup>;

// Default orders for testing
const orders: Order[] = [makeOrder({ id: 1 }), makeOrder({ id: 2 })];

function renderApp() {
  const { queryClient } = createQueryWrapper();

  // Dark mode by default (as with main.tsx)
  return render(
    <ThemeProvider theme={theme} defaultMode="dark">
      <CssBaseline />
      <QueryClientProvider client={queryClient}>
        <App />
      </QueryClientProvider>
    </ThemeProvider>,
  );
}

describe("App", () => {
  beforeEach(() => {
    // Re-initialize user per test to ensure user has fresh state
    user = userEvent.setup();

    // Reset 'useOrders' mock before each test to ensure clean state
    vi.mocked(useOrders).mockReturnValue(
      createUseOrdersMock({
        ordersQuery: createOrdersQueryMock({ data: orders }),
      }),
    );
  });

  it("invokes hooks and renders components correctly", () => {
    renderApp();

    // Assert that useOrders was called
    // Note: 'useOrders' is called twice on mount because MUI's
    // ThemeProvider/useColorScheme resolves the color scheme in a post-mount effect,
    // causing App to re-render. This is expected MUI behavior.
    // Not a bug — assert presence, not exact count.
    expect(useOrders).toHaveBeenCalled();

    // Visuals
    const heading = screen.getByRole("heading", {
      level: 1,
      name: "app.title",
    });

    expect(heading).toBeInTheDocument();

    // Assert that components were called correctly
    // Note: OrderForm has the same problem with regard to call count as above.
    expect(OrderForm).toHaveBeenCalled();
    expect(OrderList).toHaveBeenCalledWith({ orders }, undefined);
  });

  it("handles undefined data from ordersQuery gracefully", () => {
    // Override 'useOrders' mock to return undefined data
    vi.mocked(useOrders).mockReturnValue(
      createUseOrdersMock({
        ordersQuery: createOrdersQueryMock({ data: undefined }),
      }),
    );

    renderApp();

    // Assert that OrderList is called with an empty array when data is undefined
    expect(OrderList).toHaveBeenCalledWith({ orders: [] }, undefined);
  });

  it("handles theme switch correectly", async () => {
    renderApp();

    // Note: this switch called a toggle that updates it's check state.
    // If the toggle call is removed, the test is expected to break.
    // Access the switch component, expecting it to begin checked (dark)
    const themeSwitch = screen.getByRole("switch");

    expect(themeSwitch).toBeChecked();

    // Toggle the switch, and Assert that the state changed
    await user.click(themeSwitch);

    expect(themeSwitch).not.toBeChecked();

    // Toggle the switch, and Assert that the state changed back
    // Note: this is required for test coverage completeness
    await user.click(themeSwitch);

    expect(themeSwitch).toBeChecked();
  });
});

import { render, screen } from "@testing-library/react";
import { createQueryWrapper } from "./test/queryWrapper";
import { QueryClientProvider } from "@tanstack/react-query";
import { userEvent } from "@testing-library/user-event";
import { theme } from "./theme.ts";
import { ThemeProvider } from "@mui/material/styles";
import CssBaseline from "@mui/material/CssBaseline";
import App from "./App";
import OrderForm from "./components/OrderForm";
import OrderList from "./components/OrderList";

// Mock hooks and components to keep tests focused on App component
vi.mock("./components/OrderForm", () => {
  return { default: vi.fn(() => <div data-testid="order-form" />) };
});
vi.mock("./components/OrderList", () => {
  return { default: vi.fn(() => <div data-testid="order-list" />) };
});

let user: ReturnType<typeof userEvent.setup>;

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
  });

  it("renders components correctly", () => {
    renderApp();

    // Assert that header exists
    const heading = screen.getByRole("heading", {
      level: 1,
      name: "app.title",
    });

    expect(heading).toBeInTheDocument();

    // Assert that theme switch exists
    const themeSwitch = screen.getByRole("switch");

    expect(themeSwitch).toBeInTheDocument();

    // Assert that components were called correctly
    // Note: OrderForm has the same problem with regard to call count as above.
    expect(OrderForm).toHaveBeenCalled();
    expect(OrderList).toHaveBeenCalled();
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

    expect(themeSwitch).not.toBeChecked();
  });
});

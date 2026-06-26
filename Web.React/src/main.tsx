import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import App from "./App.tsx";
import "./index.css";

// Note: should be outside render to run only once
// If it's inside render it''ll keep getting invalidated
const queryClient = new QueryClient();

createRoot(document.getElementById("root")!).render(
  <StrictMode>
	  <QueryClientProvider client={queryClient}>
	    <App />
	  </QueryClientProvider>
  </StrictMode>,
)

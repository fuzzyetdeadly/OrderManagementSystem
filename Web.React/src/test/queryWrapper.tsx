import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type { ReactNode } from "react";

// React query client configured for testing
// Exposes a wrapper component that provides the client to any React tree
export function createQueryWrapper() {
  // For tests, client should not retry on failures
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
      },
      mutations: {
        retry: false,
      },
    },
  });

  const wrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );

  // Client allows access to cache, and wrapper is used to render
  // components in the same React Query context
  return { queryClient, wrapper };
}

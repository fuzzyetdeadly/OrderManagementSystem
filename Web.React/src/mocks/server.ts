import { setupServer } from "msw/node";
import { orderHandlers } from "./orderHandlers";

// Spin up a mock server for testing with defined handlers
export const server = setupServer(...orderHandlers);

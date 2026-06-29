// Define 'msw' fake server returns'
import { http, HttpResponse } from "msw";
import type { Order } from "../types/order";
import { makeOrder } from "./factories/orderFactory";

const baseURL = import.meta.env.VITE_API_URL;

// Handlers contain happy behaviors for API calls
// These may be overridden in test files to simulate error conditions, etc.
export const orderHandlers = [
  http.get(`${baseURL}/orders`, () => {
    return HttpResponse.json<Order[]>([makeOrder()]);
  }),

  http.post(`${baseURL}/orders`, () => {
    return HttpResponse.json<Order>(makeOrder(), { status: 201 });
  }),

  http.patch(`${baseURL}/orders/:id/status`, () => {
    return HttpResponse.json<Order>(makeOrder({ status: "Processing" }));
  }),

  http.delete(`${baseURL}/orders/:id`, () => {
    return HttpResponse.text(null, { status: 204 });
  }),
];

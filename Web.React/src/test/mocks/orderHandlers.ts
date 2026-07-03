// Define 'msw' fake server returns'
import { http, HttpResponse } from "msw";
import type {
  Order,
  CreateOrderPayload,
  UpdateOrderStatusPayload,
} from "../../types/order";
import { makeOrder } from "../factories/orderFactory";

const baseURL = import.meta.env.VITE_API_URL;

// Handlers contain happy behaviors for API calls
// Behaviors may be overridden in test files to simulate error conditions, etc.
export const orderHandlers = [
  http.get(`${baseURL}/orders`, () => {
    return HttpResponse.json<Order[]>([makeOrder()]);
  }),

  http.post(`${baseURL}/orders`, async ({ request }) => {
    // No clone is safe here, as handler expected to be final consumer of body
    const payload = (await request.json()) as CreateOrderPayload;
    const createdOrder = makeOrder({
      customerId: payload.customerId,
      items: payload.items,
    });

    return HttpResponse.json<Order>(createdOrder, { status: 201 });
  }),

  http.patch(`${baseURL}/orders/:id/status`, async ({ request }) => {
    const payload = (await request.json()) as UpdateOrderStatusPayload;
    const updatedOrder = makeOrder({ status: payload.status });

    return HttpResponse.json<Order>(updatedOrder);
  }),

  http.delete(`${baseURL}/orders/:id`, () => {
    // Note: null return here = empty body
    return HttpResponse.text(null, { status: 204 });
  }),
];

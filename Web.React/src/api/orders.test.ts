import { server } from "../mocks/server";
import { getOrders, createOrder, updateOrderStatus } from "./orders";
import type { PaginationOptions } from "../types/common";
import type {
  CreateOrderPayload,
  UpdateOrderStatusPayload,
} from "../types/order";
import { makeOrder } from "../mocks/factories/orderFactory";

// Note: most coverage seems to be just checking that body/params forward correctly
// Not meaningful to test mocked return values (would just be testing axios behavior))
describe("getOrders", () => {
  it("uses the correct route", async () => {
    let capturedRoute: string | null = null;

    // Make server listerner capture request route
    server.events.on("request:start", ({ request }) => {
      capturedRoute = new URL(request.url).pathname;
    });

    await getOrders();

    // Expect route to end with "/orders" (regex)
    expect(capturedRoute).toMatch(/\/orders$/);
  });

  it("sends no pagination params", async () => {
    let capturedParams: URLSearchParams | null = null;

    // Capture params passed to the API with "request:start" event
    server.events.on("request:start", ({ request }) => {
      capturedParams = new URL(request.url).searchParams;
    });

    await getOrders();

    // Note: expect 'capturedParams' never to be null
    expect(capturedParams!.has("page")).toBe(false);
    expect(capturedParams!.has("pageSize")).toBe(false);
  });

  it("sends only page param when pageSize omitted", async () => {
    let capturedParams: URLSearchParams | null = null;

    // Capture params passed to the API with "request:start" event
    server.events.on("request:start", ({ request }) => {
      capturedParams = new URL(request.url).searchParams;
    });

    const params: PaginationOptions = { page: 2 };

    await getOrders(params);

    // Note: expect 'capturedParams' never to be null
    expect(capturedParams!.get("page")).toBe("2");
    expect(capturedParams!.has("pageSize")).toBe(false);
  });

  it("sends only pageSize param when page omitted", async () => {
    let capturedParams: URLSearchParams | null = null;

    // Capture params passed to the API with "request:start" event
    server.events.on("request:start", ({ request }) => {
      capturedParams = new URL(request.url).searchParams;
    });

    const params: PaginationOptions = { pageSize: 10 };

    await getOrders(params);

    // Note: expect 'capturedParams' never to be null
    expect(capturedParams!.has("page")).toBe(false);
    expect(capturedParams!.get("pageSize")).toBe("10");
  });

  it("sends both pagination params", async () => {
    let capturedParams: URLSearchParams | null = null;

    // Capture params passed to the API with "request:start" event
    server.events.on("request:start", ({ request }) => {
      capturedParams = new URL(request.url).searchParams;
    });

    const params: PaginationOptions = { page: 2, pageSize: 10 };

    await getOrders(params);

    // Note: expect 'capturedParams' never to be null
    expect(capturedParams!.get("page")).toBe("2");
    expect(capturedParams!.get("pageSize")).toBe("10");
  });

  it("returns expected data shape", async () => {
    const orders = await getOrders();

    // Expect return to match mocked order array
    expect(orders).toEqual([makeOrder()]);
  });
});

describe("createOrder", () => {
  it("uses the correct route", async () => {
    let capturedRoute: string | null = null;

    // Make server listerner capture request route
    server.events.on("request:start", ({ request }) => {
      capturedRoute = new URL(request.url).pathname;
    });

    // Payload is a don't care
    const payload: CreateOrderPayload = {
      customerId: 1,
      items: [],
    };

    await createOrder(payload);

    // Expect route to end with "/orders" (regex)
    expect(capturedRoute).toMatch(/\/orders$/);
  });

  it("sends the request payload", async () => {
    let capturedBody: CreateOrderPayload | null = null;

    server.events.on("request:start", async ({ request }) => {
      // Clone required to read body without consuming original request
      const clone = request.clone();
      capturedBody = await clone.json();
    });

    // Post a dummy payload to the API
    const payload: CreateOrderPayload = {
      customerId: 1,
      items: [
        {
          productName: "Potato",
          quantity: 1,
          unitPrice: 0.99,
        },
      ],
    };

    await createOrder(payload);

    // Expect captured body to match payload (deep equality)
    expect(capturedBody).toEqual(payload);
  });

  it("returns expected data shape", async () => {
    // Payload is a don't care
    const payload: CreateOrderPayload = {
      customerId: 1,
      items: [],
    };

    const order = await createOrder(payload);

    // Expect return to match mocked order
    expect(order).toEqual(makeOrder());
  });
});

describe("updateOrderStatus", () => {
  it("uses the correct route", async () => {
    let capturedRoute: string | null = null;

    // Make server listerner capture request route
    server.events.on("request:start", ({ request }) => {
      capturedRoute = new URL(request.url).pathname;
    });

    // Update order 1. Payload is don't care
    const id = 1;
    const payload: UpdateOrderStatusPayload = { status: "Processing" };

    await updateOrderStatus(id, payload);

    // Expect route to end with "/orders" (regex)
    expect(capturedRoute).toMatch(new RegExp(`\\/orders\\/${id}\\/status$`));
  });

  it("sends the request payload", async () => {
    let capturedBody: CreateOrderPayload | null = null;

    server.events.on("request:start", async ({ request }) => {
      // Clone required to read body without consuming original request
      const clone = request.clone();
      capturedBody = await clone.json();
    });

    // Update order 1 status
    const id = 1;
    const payload: UpdateOrderStatusPayload = { status: "Processing" };

    await updateOrderStatus(id, payload);

    // Expect captured body to match payload (deep equality)
    expect(capturedBody).toEqual(payload);
  });

  it("returns expected data shape", async () => {
    // Update order 1
    // Payload is don't care (handler always returns "Processing" status)
    const id = 1;
    const payload: UpdateOrderStatusPayload = { status: "Processing" };

    const order = await updateOrderStatus(id, payload);

    // Expect return to match mocked order with "Processing" status
    expect(order).toEqual(makeOrder({ status: "Processing" }));
  });
});

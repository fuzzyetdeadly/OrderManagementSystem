import { server } from "../mocks/server";
import { getOrders } from "./orders";
import type { PaginationOptions } from "../types/common";

describe("getOrders", () => {
  it("returns orders on success", async () => {
    const result = await getOrders();

    // Expect happy return value
    expect(result).toEqual([
      {
        id: 1,
        status: "Pending",
        created: "2023-01-01T12:00:00Z",
        customerId: 1,
        items: [{ productName: "Potato", quantity: 1, unitPrice: 0.99 }],
      },
    ]);
  });

  it("passes pagination parameters to the API", async () => {
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
});

// ToCheck: For create order, should request body be forwarded to test server handler?
// and used as return value with id appended?

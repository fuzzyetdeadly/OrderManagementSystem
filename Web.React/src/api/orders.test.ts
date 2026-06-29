import { server } from "../mocks/server";
import { getOrders } from "./orders";
import type { PaginationOptions } from "../types/common";

// Note: most coverage seems to be just checking that body/params forward correctly
// Not meaningful to test mocked return values (would just be testing axios behavior))
describe("getOrders", () => {
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

// TBC: test that Post forwards payload correctly

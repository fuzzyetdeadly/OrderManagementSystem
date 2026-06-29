import { server } from "./mocks/server";

// For every test file, start the MSW server, reset between tests, and close after
beforeAll(() => server.listen());
afterEach(() => server.resetHandlers());
afterAll(() => server.close());

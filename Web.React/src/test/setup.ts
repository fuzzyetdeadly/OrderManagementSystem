import "@testing-library/jest-dom/vitest"; // executes modules code for side-effects
import { server } from "./mocks/server";

// Intercept module and override 'useTranslation' 't' function to return keys as value
vi.mock("react-i18next", () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

// For every test file, start the MSW server, reset between tests, and close after
beforeAll(() => server.listen());
afterEach(() => {
  server.resetHandlers();
  server.events.removeAllListeners();
});
afterAll(() => server.close());

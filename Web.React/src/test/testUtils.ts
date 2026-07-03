import { screen, within } from "@testing-library/react";

export function getRow(name: RegExp | string) {
  return screen.getByRole("row", { name });
}

export function getButton(container: HTMLElement, name: RegExp | string) {
  return within(container).getByRole("button", { name });
}

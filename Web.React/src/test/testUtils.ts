import { screen, within } from "@testing-library/react";
import type { UserEvent } from "@testing-library/user-event";

export function getRow(name: RegExp | string) {
  return screen.getByRole("row", { name });
}

export function getButton(container: HTMLElement, name: RegExp | string) {
  return within(container).getByRole("button", { name });
}

// Helper method to select an option in an MUI ListBox
export async function selectListOption(
  user: UserEvent,
  muiListBox: HTMLElement,
  optionName: string,
) {
  // Click the listbox to expose it's options
  await user.click(muiListBox);

  // No check here. Expect failure if role not found
  const option = await screen.findByRole("option", { name: optionName });
  await user.click(option);
}

import { createTheme } from "@mui/material/styles";

export const theme = createTheme({
  colorSchemes: {
    light: {
      palette: {
        background: {
          default: "#F5F5F3", // soft warm gray
          paper: "#FAFAF8", // lighter, for Card/Paper/AppBar
        },
      },
    },
    dark: true,
  },
  cssVariables: {
    colorSchemeSelector: "class",
  },
});

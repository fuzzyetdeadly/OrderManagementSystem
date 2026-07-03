import i18n from "i18next";
import { initReactI18next } from "react-i18next";
import HttpBackend from "i18next-http-backend";
import LanguageDetector from "i18next-browser-languagedetector";

// Bootstrap logic for i18n translations. Imported once at main.tsx
// 'init' returns a Promise, which is why 'export default' directly used here
i18n
  .use(HttpBackend)
  .use(LanguageDetector)
  .use(initReactI18next) // Exposes i18n to React
  .init({
    fallbackLng: "en",
    debug: import.meta.env.DEV,
    backend: { loadPath: "/locales/{{lng}}/{{ns}}.json" },
    interpolation: { escapeValue: false }, // React already escapes
  });

// Exports actual i18n singleton instance
export default i18n;

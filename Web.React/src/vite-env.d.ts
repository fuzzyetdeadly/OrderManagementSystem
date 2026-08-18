/// <reference types="vite/client" />

// These typings are to ensure intellisense recognizes custom variables
// Custom environment variables (VITE prefixed = exposed to client)
interface ImportMetaEnv {
	readonly VITE_API_URL: string;
	readonly VITE_API_TIMEOUT_MS: string;
}

interface ImportMeta {
	readonly env: ImportMetaEnv;
}
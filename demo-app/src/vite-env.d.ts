/// <reference types="vite/client" />

// These typings are to ensure intellisense recognizes custom variables
// Custom environment variables (VITE prefixed = exposed to client)
interface ImportMetaEnv {
	readonly VITE_API_BASE_URL: string;
}

interface ImportMeta {
	readonly env: ImportMetaEnv;
}
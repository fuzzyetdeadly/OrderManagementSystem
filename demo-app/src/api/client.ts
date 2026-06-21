import axios from "axios";

// Note: env vars are always string by default (hard constraint of Vite)
const apiUrl = import.meta.env.VITE_API_URL;
const timeoutEnv = import.env.VITE_API_TIMEOUT_MS;
const timeout = Number(timeoutEnv) || 10000;

if(!apiUrl) {
	throw new Error("VITE_API_URL not set - check .env(.*?) file for current run mode");
}

export const api = axios.create({ baseURL, timeout });
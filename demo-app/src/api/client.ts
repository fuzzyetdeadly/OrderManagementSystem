import axios from "axios";

const baseURL = import.meta.env.VITE_API_BASE_URL;

if(!baseURL) {
	throw new Error("VITE_API_BASE_URL not set - check .env(.*?) file for current run mode");
}

export const api = axios.create({ baseURL });
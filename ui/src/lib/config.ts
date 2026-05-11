export const REST_API_BASE_URL =
  import.meta.env.VITE_REST_API_BASE_URL ?? "http://localhost:5001";

export const SIGNALR_TELEMETRY_HUB_URL = `${REST_API_BASE_URL}/hubs/telemetry`;


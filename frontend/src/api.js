// Backend URL comes from the environment (never hardcoded in components).
const BASE = import.meta.env.VITE_API_URL || "http://localhost:8080";

export class ApiError extends Error {
  constructor(status, code, message) {
    super(message);
    this.status = status;
    this.code = code;
  }
}

async function request(path, { method = "GET", body, studentId } = {}) {
  const headers = {};
  if (body !== undefined) headers["Content-Type"] = "application/json";
  if (studentId) headers["X-Student-Id"] = studentId;

  let res;
  try {
    res = await fetch(BASE + path, {
      method,
      headers,
      body: body !== undefined ? JSON.stringify(body) : undefined,
    });
  } catch {
    throw new ApiError(0, "NETWORK_ERROR", "Cannot reach the server. Check your connection and try again.");
  }

  if (res.status === 204) return null;

  let data = null;
  try {
    data = await res.json();
  } catch {
    /* response had no JSON body */
  }

  if (!res.ok) {
    const fallback =
      res.status >= 500 ? "Server error. Please try again in a moment." : "The request failed.";
    throw new ApiError(res.status, data?.code ?? "ERROR", data?.message ?? fallback);
  }
  return data;
}

export const errorText = (e) => (e instanceof Error ? e.message : "Something went wrong.");

export const api = {
  getClasses: () => request("/classes"),
  createClass: (name, totalSeats) => request("/classes", { method: "POST", body: { name, totalSeats } }),
  apply: (classId, studentId) => request(`/classes/${classId}/holds`, { method: "POST", studentId }),
  confirm: (holdId, studentId) => request(`/holds/${holdId}/confirm`, { method: "POST", studentId }),
  cancel: (holdId, studentId) => request(`/holds/${holdId}`, { method: "DELETE", studentId }),
  myHolds: (studentId) => request("/me/holds", { studentId }),
};
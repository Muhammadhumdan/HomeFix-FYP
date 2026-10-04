import axios from 'axios';

export const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5080/api';
const api = axios.create({ baseURL: API_URL });

// Attach the JWT to every request
api.interceptors.request.use((cfg) => {
  const token = localStorage.getItem('hf_token');
  if (token) cfg.headers.Authorization = `Bearer ${token}`;
  return cfg;
});

// Expired / invalid token -> log out and go to login
api.interceptors.response.use(
  (r) => r,
  (err) => {
    if (err.response?.status === 401 && localStorage.getItem('hf_token')) {
      localStorage.removeItem('hf_token');
      localStorage.removeItem('hf_user');
      window.location.href = '/login?expired=1';
    }
    return Promise.reject(err);
  }
);

// Turn any axios error into a message that can be shown to the user
export function errMsg(err) {
  if (!err.response) return `Cannot reach the HomeFix server. Make sure the API is running (${API_URL}).`;
  const { status, data } = err.response;
  if (status === 403) return data?.message || 'You do not have permission to do that.';
  if (status === 401) return data?.message || 'Please log in to continue.';
  return data?.message || data?.title || `Request failed (${status}).`;
}

export default api;

import axios from 'axios';
import { useAppStore } from '../app/store/useAppStore';

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5001',
  withCredentials: true, // sends HttpOnly ep_refresh cookie automatically
});

// ── Request interceptor: attach access token from in-memory store ─────────
apiClient.interceptors.request.use((config) => {
  const { accessToken } = useAppStore.getState();
  if (accessToken) {
    config.headers.Authorization = `Bearer ${accessToken}`;
  }
  return config;
});

// ── Response interceptor: on 401 wipe session and force re-login ──────────
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      useAppStore.getState().clearSession();
      // Navigate outside React tree — replace so Back button doesn't return to a broken page
      window.location.replace('/login');
    }
    return Promise.reject(error);
  },
);

export default apiClient;

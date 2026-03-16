import axios from 'axios';
import { useAppStore } from '../app/store/useAppStore';

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5001',
  withCredentials: true,
});

apiClient.interceptors.request.use((config) => {
  const { accessToken } = useAppStore.getState();
  if (accessToken) {
    config.headers.Authorization = `Bearer ${accessToken}`;
  }
  return config;
});

apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (error.response?.status === 401) {
      const { clearSession } = useAppStore.getState();
      clearSession();
    }
    return Promise.reject(error);
  }
);

export default apiClient;

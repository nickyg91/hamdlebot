import type { ITwitchOAuthToken } from '@/models/twitch-oauth-token.interface';
import axios, { type AxiosInstance } from 'axios';
const httpClient: AxiosInstance = axios.create({
  baseURL: '/api'
});
export const useAxios = () => {
  const addTokenInterceptor = (token: string) => {
    httpClient.interceptors.request.use((config) => {
      config.headers.Authorization = `Bearer ${token}`;
      return config;
    });
  };
  const addRefreshTokenInterceptor = (refreshToken: string) => {
    httpClient.interceptors.response.use(
      (response) => response,
      async (error) => {
        const originalRequest = error.config;
        if (error.response?.status === 401) {
          originalRequest._retry = true;
          const response = await httpClient.get<ITwitchOAuthToken>(
            `/twitch/auth/token/refresh/${refreshToken}`
          );
          if (response.status === 200) {
            addTokenInterceptor(response.data.id_token);
            addRefreshTokenInterceptor(response.data.refresh_token);
            return httpClient(originalRequest);
          }
        }
        return Promise.reject(error);
      }
    );
  };
  return {
    httpClient,
    addTokenInterceptor,
    addRefreshTokenInterceptor
  };
};

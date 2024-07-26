import type { ITwitchOAuthToken } from '@/models/auth/twitch-oauth-token.interface';
import axios, { type AxiosInstance } from 'axios';
import { useToast } from 'primevue/usetoast';

const httpClient: AxiosInstance = axios.create({
  baseURL: '/api'
});

export const useAxios = () => {
  const toast = useToast();
  //global because lazy. This is not a good practice.
  const addErrorInterceptor = () => {
    httpClient.interceptors.response.use(
      (response) => {
        toast.add({
          severity: 'success',
          summary: 'Request successful.',
          detail: 'The request was processed successfully.',
          life: 5000,
          closable: true
        });
        return response;
      },
      (error: Error) => {
        toast.add({
          severity: 'error',
          summary: 'An error occurred while processing your request.',
          detail: error.message,
          life: 3000,
          closable: true
        });
        throw error;
      }
    );
  };
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
    addRefreshTokenInterceptor,
    addErrorInterceptor
  };
};

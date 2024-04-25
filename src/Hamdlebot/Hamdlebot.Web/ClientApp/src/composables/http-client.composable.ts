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

  return {
    httpClient,
    addTokenInterceptor
  };
};

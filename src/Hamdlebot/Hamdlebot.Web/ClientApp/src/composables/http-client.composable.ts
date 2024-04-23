import axios, { type AxiosInstance } from 'axios';
const instance: AxiosInstance = axios.create({
  baseURL: '/api'
});
export const useAxios = () => {
  const addTokenInterceptor = (token: string) => {
    instance.interceptors.request.use((config) => {
      config.headers.Authorization = `Bearer ${token}`;
      return config;
    });
  };

  return {
    instance,
    addTokenInterceptor
  };
};

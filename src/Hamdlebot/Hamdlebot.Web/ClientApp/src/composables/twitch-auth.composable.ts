import { useAxios } from './http-client.composable';

export const useTwitchAuthService = () => {
  const httpClient = useAxios();
  const getTwitchAuthUrl = async (): Promise<string> => {
    return (await httpClient.get('/twitch/auth')).data;
  };

  const setCode = async (code: string): Promise<string> => {
    return await httpClient.put(`/twitch/code/${code}`);
  };

  return {
    getTwitchAuthUrl,
    setCode
  };
};

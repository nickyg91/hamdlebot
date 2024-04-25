import type { ITwitchOAuthToken } from '@/models/twitch-oauth-token.interface';
import { useAxios } from './http-client.composable';

export const useTwitchAuthService = () => {
  const { httpClient } = useAxios();
  const getTwitchAuthUrl = async (): Promise<string> => {
    return (await httpClient.get('/twitch/auth')).data;
  };

  const getTwitchTokenAuthUrl = async (): Promise<string> => {
    return (await httpClient.get('/twitch/auth/oidc/url')).data;
  };

  const getTwitchOAuthToken = async (code: string): Promise<ITwitchOAuthToken> => {
    return (await httpClient.get(`/twitch/auth/token/${code}`)).data;
  };

  const testAuth = async (): Promise<string> => {
    return (await httpClient.get('/twitch/auth/test')).data;
  };

  return {
    getTwitchAuthUrl,
    getTwitchOAuthToken,
    getTwitchTokenAuthUrl,
    testAuth
  };
};

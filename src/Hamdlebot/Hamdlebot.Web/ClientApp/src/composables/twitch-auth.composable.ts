import type { ITwitchOAuthToken } from '@/models/auth/twitch-oauth-token.interface';
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

  const getTwitchBotOAuthToken = async (code: string): Promise<ITwitchOAuthToken> => {
    return (await httpClient.get(`/twitch/auth/token/bot/${code}`)).data;
  };

  return {
    getTwitchAuthUrl,
    getTwitchOAuthToken,
    getTwitchTokenAuthUrl,
    getTwitchBotOAuthToken
  };
};

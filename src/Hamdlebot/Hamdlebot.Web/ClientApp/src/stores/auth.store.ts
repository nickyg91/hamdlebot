import { useTwitchAuthService } from '@/composables/twitch-auth.composable';
import type { ITwitchOAuthToken } from '@/models/twitch-oauth-token.interface';
import { defineStore } from 'pinia';
import { ref } from 'vue';

export const useAuthStore = defineStore('auth', () => {
  const twitchAuthService = useTwitchAuthService();
  const token = ref<ITwitchOAuthToken | null>(null);

  const getTwitchAuthUrl = async (): Promise<string> => {
    return await twitchAuthService.getTwitchAuthUrl();
  };

  const getTwitchOAuthToken = async (code: string): Promise<ITwitchOAuthToken> => {
    return await twitchAuthService.getTwitchOAuthToken(code);
  };

  const getTwitchOIDCUrl = async (): Promise<string> => {
    return await twitchAuthService.getTwitchTokenAuthUrl();
  };

  return {
    token,
    getTwitchAuthUrl,
    getTwitchOAuthToken,
    getTwitchOIDCUrl
  };
});

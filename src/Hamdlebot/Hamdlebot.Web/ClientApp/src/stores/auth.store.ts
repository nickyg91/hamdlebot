import { useTwitchAuthService } from '@/composables/twitch-auth.composable';
import type { ITwitchOAuthToken } from '@/models/auth/twitch-oauth-token.interface';
import type { IUserInfo } from '@/models/auth/user-info.interface';
import { defineStore } from 'pinia';
import { computed, ref, type ComputedRef } from 'vue';

export const useAuthStore = defineStore('auth', () => {
  const hamdlebotId = '978363539';
  const twitchAuthService = useTwitchAuthService();
  const token = ref<ITwitchOAuthToken | null>(null);
  const getTwitchAuthUrl = async (): Promise<string> => {
    return await twitchAuthService.getTwitchAuthUrl();
  };

  const jwtDecoded: ComputedRef<IUserInfo | null> = computed(() => {
    if (token.value) {
      return decodeToken(token.value.id_token);
    }
    return null;
  });

  const isHamdlebot = computed(() => {
    return jwtDecoded.value?.sub === hamdlebotId;
  });

  const getTwitchOAuthToken = async (code: string): Promise<ITwitchOAuthToken> => {
    return await twitchAuthService.getTwitchOAuthToken(code);
  };

  const getTwitchOIDCUrl = async (): Promise<string> => {
    return await twitchAuthService.getTwitchTokenAuthUrl();
  };

  const getTwitchBotOAuthToken = async (code: string): Promise<ITwitchOAuthToken> => {
    return await twitchAuthService.getTwitchBotOAuthToken(code);
  };

  const decodeToken = (token: string): IUserInfo => {
    const base64Url = token.split('.')[1];
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    const jsonPayload = decodeURIComponent(
      window
        .atob(base64)
        .split('')
        .map(function (c) {
          return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
        })
        .join('')
    );

    return JSON.parse(jsonPayload);
  };

  return {
    token,
    jwtDecoded,
    isHamdlebot,
    getTwitchAuthUrl,
    getTwitchOAuthToken,
    getTwitchOIDCUrl,
    getTwitchBotOAuthToken
  };
});

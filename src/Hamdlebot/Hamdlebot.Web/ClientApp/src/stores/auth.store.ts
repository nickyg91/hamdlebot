import { useAxios } from '@/composables/http-client.composable';
import type { ITwitchOAuthToken } from '@/models/twitch-oauth-token.interface';
import { defineStore } from 'pinia';
import { ref } from 'vue';

export const useAuthStore = defineStore('auth', () => {
  const httpClient = useAxios();

  const token = ref<ITwitchOAuthToken | null>();
  return {
    token
  };
});

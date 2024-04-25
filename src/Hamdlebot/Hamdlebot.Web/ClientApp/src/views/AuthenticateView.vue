<script setup lang="ts">
import { useAxios } from '@/composables/http-client.composable';
import { useTwitchAuthService } from '@/composables/twitch-auth.composable';
import { useAuthStore } from '@/stores/auth.store';
import { onMounted } from 'vue';
import { useRouter } from 'vue-router';

const router = useRouter();
const { getTwitchOAuthToken } = useTwitchAuthService();
const httpClient = useAxios();
const authStore = useAuthStore();
onMounted(async () => {
  if (router.currentRoute.value.query.code) {
    const token = await getTwitchOAuthToken(router.currentRoute.value.query.code as string);
    if (token) {
      try {
        console.log(await authStore.testAuth());
      } catch (error) {
        console.error('Expected');
      }
      httpClient.addTokenInterceptor(token.id_token);
      console.log(await authStore.testAuth());
      router.push({ name: 'dashboard' });
      authStore.token = token;
    } else {
      console.error('Failed to get Twitch OAuth token');
    }
  }
});
router.currentRoute.value.query;
</script>
<template>
  <div></div>
</template>

<style scoped></style>

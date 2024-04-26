<script setup lang="ts">
import { useAxios } from '@/composables/http-client.composable';
import { useTwitchAuthService } from '@/composables/twitch-auth.composable';
import { useAuthStore } from '@/stores/auth.store';
import { onMounted } from 'vue';
import { useRouter } from 'vue-router';
import ProgressSpinner from 'primevue/progressspinner';

const router = useRouter();
const { getTwitchOAuthToken } = useTwitchAuthService();
const httpClient = useAxios();
const authStore = useAuthStore();
onMounted(async () => {
  if (router.currentRoute.value.query.code) {
    const token = await getTwitchOAuthToken(router.currentRoute.value.query.code as string);
    if (token) {
      httpClient.addTokenInterceptor(token.id_token);
      router.push({ name: 'dashboard' });
      authStore.token = token;
    } else {
      console.error('Failed to get Twitch OAuth token');
    }
  }
});
</script>
<template>
  <Suspense>
    <div class="flex justify-content-center">
      <div class="flex-column">
        <div>
          <h1>Please wait while we authenticate to Twitch</h1>
        </div>
        <div class="flex justify-content-center">
          <ProgressSpinner />
        </div>
      </div>
    </div>
  </Suspense>
</template>

<style scoped></style>

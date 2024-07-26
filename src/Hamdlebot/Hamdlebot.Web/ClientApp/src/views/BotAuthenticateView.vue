<script setup lang="ts">
import { onMounted, ref, watch } from 'vue';
import { useAuthStore } from '@/stores/auth.store';
import { useRouter } from 'vue-router';
import type { ITwitchOAuthToken } from '@/models/auth/twitch-oauth-token.interface';
const authStore = useAuthStore();
const router = useRouter();
const token = ref<ITwitchOAuthToken | null>(null);
const isLoading = ref(false);
watch(
  token,
  () => {
    if (token.value) {
      isLoading.value = false;
    }
  },
  { immediate: true }
);
onMounted(async () => {
  const code = router.currentRoute.value.query.code as string;
  try {
    if (code) {
      await authStore.getTwitchBotOAuthToken(code);
    }
  } catch (error) {
    console.error(error);
  }
});
</script>
<template>
  <div>
    <Suspense>
      <div class="p-5 flex justify-content-center">
        <div class="flex-column">
          <div>
            <h1 v-if="isLoading">Please wait while we authenticate to Twitch.</h1>
            <h1 v-else>Authentication to Twitch is complete. You may close this window.</h1>
          </div>
          <div v-if="isLoading" class="flex justify-content-center">
            <ProgressSpinner />
          </div>
        </div>
      </div>
    </Suspense>
  </div>
</template>

<style scoped></style>

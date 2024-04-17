<script setup lang="ts">
import { useTwitchAuthService } from '@/composables/twitch-auth.composable';
import Button from 'primevue/button';
import { onMounted } from 'vue';

const twitchAuthService = useTwitchAuthService();
const getAuthUrl = async () => {
  const authUrl = await twitchAuthService.getTwitchAuthUrl();
  window.location.replace(authUrl);
  //window.location.href = authUrl;
};

onMounted(async () => {
  // const code = router.currentRoute.value.query['code'] as string;
  // if (code) {
  //   await twitchAuthService.setCode(code);
  // }
});
</script>
<template>
  <div class="p-5">
    <div class="flex justify-content-center">
      <Button label="Authenticate" @click="getAuthUrl"></Button>
    </div>
  </div>
</template>

<style scoped></style>

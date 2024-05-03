<script setup lang="ts">
import { RouterView } from 'vue-router';
import Toast from 'primevue/toast';
import { onMounted } from 'vue';
import { useAxios } from './composables/http-client.composable';
const http = useAxios();
onMounted(() => {
  http.addErrorInterceptor();
});
</script>

<template>
  <section>
    <RouterView />
    <Toast />
    <Toast group="signalr" position="top-center" />
  </section>
</template>

<style scoped></style>

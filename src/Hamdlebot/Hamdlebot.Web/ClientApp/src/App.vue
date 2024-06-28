<script setup lang="ts">
import { RouterView } from 'vue-router';
import Toast from 'primevue/toast';
import { onMounted } from 'vue';
import { useAxios } from './composables/http-client.composable';
import DynamicDialog from 'primevue/dynamicdialog';
const http = useAxios();
onMounted(() => {
  http.addErrorInterceptor();
});
</script>

<template>
  <section class="min-h-screen">
    <RouterView />
    <Toast />
    <Toast group="signalr" />
    <DynamicDialog />
  </section>
</template>

<style scoped></style>

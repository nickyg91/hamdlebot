<script setup lang="ts">
import { onBeforeMount } from 'vue';
import { RouterView } from 'vue-router';
import { useSignalR } from './composables/signalr.composable';
const { createSignalRConnection } = useSignalR();
onBeforeMount(async () => {
  await createSignalRConnection('botloghub');
  await createSignalRConnection('hamdlebothub');
});
</script>

<template>
  <RouterView />
</template>

<style scoped></style>

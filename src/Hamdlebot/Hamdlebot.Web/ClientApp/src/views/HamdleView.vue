<script setup lang="ts">
import WordGuess from '@/components/WordGuess.vue';
import { useHamdleStore } from '@/stores/hamdle.store';
import { onMounted } from 'vue';

const store = useHamdleStore();
onMounted(async () => {
  await store.createSignalRConnection();
});
</script>
<template>
  <div>
    <WordGuess :current-word="store.currentWord"></WordGuess>
  </div>
</template>

<style scoped></style>

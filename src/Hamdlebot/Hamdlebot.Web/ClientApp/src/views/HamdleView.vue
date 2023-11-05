<script setup lang="ts">
import WordGuess from '@/components/WordGuess.vue';
import { useHamdleStore } from '@/stores/hamdle.store';
import { computed, onMounted } from 'vue';

const store = useHamdleStore();
onMounted(async () => {
  await store.createSignalRConnection();
});
const guesses = computed(() => {
  const guesses = [];
  for (let i = 0; i < 5; i++) {
    if (i > store.guesses.length) {
      guesses.push(null);
    } else {
      guesses.push(store.guesses[i]);
    }
  }
  return guesses;
});
</script>
<template>
  <div>
    <div class="flex flex-column align-items-center">
      <WordGuess
        class="flex p-5 mb-5"
        v-for="(guess, index) in guesses"
        :key="index"
        :guess="guess"
        :current-word="store.currentWord"
      ></WordGuess>
    </div>
  </div>
</template>

<style scoped></style>

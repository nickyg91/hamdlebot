<script setup lang="ts">
import GuessedLetters from '@/components/hamdle/features/GuessedLetters.vue';
import HamdleTimer from '@/components/hamdle/features/HamdleTimer.vue';
import WordGuess from '@/components/hamdle/features/WordGuess.vue';
import { useHamdleStore } from '@/components/hamdle/stores/hamdle.store';
import { computed, onMounted, watchEffect } from 'vue';
import ProgressSpinner from 'primevue/progressspinner';
import { useConfetti } from '@/composables/confetti.composable';
import { useRoute } from 'vue-router';
const store = useHamdleStore();
const confetti = useConfetti();

onMounted(async () => {
  const route = useRoute();
  const twitchUserId = route.params.twitchUserId.toString();
  await store.startSignalRConnection(twitchUserId);
});

watchEffect(() => {
  if (store.showConfetti) {
    confetti.startConfetti();
  }
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
  <Suspense>
    <template #default>
      <div>
        <div class="flex flex-column align-items-center">
          <HamdleTimer class="flex p-3 mb-1 justify-content-center"></HamdleTimer>
          <WordGuess
            class="flex p-3 mb-2"
            v-for="(guess, index) in guesses"
            :key="index"
            :guess="guess"
            :current-word="store.currentWord"
          ></WordGuess>
          <GuessedLetters> </GuessedLetters>
        </div>
      </div>
    </template>
    <template #fallback>
      <ProgressSpinner />
    </template>
  </Suspense>
</template>

<style scoped></style>

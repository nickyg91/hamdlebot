<script setup lang="ts">
import { useHamdleStore } from '@/stores/hamdle.store';
import { ref, watchEffect } from 'vue';

const store = useHamdleStore();
const guessTimer = ref(0);
const votingTimer = ref(0);
const guessInterval = ref<number | null>(null);
const votingInterval = ref<number | null>(null);

watchEffect(() => {
  if (store.showGuessTimer) {
    guessTimer.value = store.guessMs / 1000;
    guessInterval.value = setInterval(() => {
      if (guessTimer.value === 0) {
        clearInterval(guessInterval.value!);
        store.resetGuessTimer();
      }
      guessTimer.value--;
    }, 1000);
  }
});

watchEffect(() => {
  if (store.showVotingTimer) {
    votingTimer.value = store.votingMs / 1000;
    votingInterval.value = setInterval(() => {
      if (votingTimer.value === 0) {
        clearInterval(votingInterval.value!);
        store.resetVotingTimer();
      }
      votingTimer.value--;
    }, 1000);
  }
});
</script>
<template>
  <div>
    <span class="timer-text" v-if="store.showGuessTimer">
      Time to guess: {{ guessTimer }} seconds
    </span>
    <span class="timer-text" v-if="store.showVotingTimer">
      Time to vote: {{ votingTimer }} seconds
    </span>
  </div>
</template>

<style scoped>
.timer-text {
  font-size: 1.75em;
}
</style>

<script setup lang="ts">
import { useHamdleStore } from '@/components/hamdle/stores/hamdle.store';
import { ref, watchEffect } from 'vue';

const store = useHamdleStore();
const guessTimer = ref(0);
const votingTimer = ref(0);
const betweenRoundsTimer = ref(0);
const guessInterval = ref<number | null>(null);
const votingInterval = ref<number | null>(null);
const betweenRoundsInterval = ref<number | null>(null);

watchEffect(() => {
  if (store.showGuessTimer) {
    guessTimer.value = store.guessMs / 1000;
    guessInterval.value = setInterval(() => {
      if (guessTimer.value === 1) {
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
      if (votingTimer.value === 1) {
        clearInterval(votingInterval.value!);
        store.resetVotingTimer();
        return;
      }
      votingTimer.value--;
    }, 1000);
  }
});

watchEffect(() => {
  if (store.showBetweenRoundMs) {
    betweenRoundsTimer.value = store.betweenRoundMs / 1000;
    betweenRoundsInterval.value = setInterval(() => {
      if (betweenRoundsTimer.value === 1) {
        clearInterval(betweenRoundsInterval.value!);
        store.resetBetweenGuessTimer();
        return;
      }
      betweenRoundsTimer.value--;
    }, 1000);
  }
});
</script>
<template>
  <div>
    <div class="timer-text" v-if="store.showGuessTimer">
      Time to guess: {{ guessTimer }} seconds
    </div>
    <div class="timer-text" v-if="store.showVotingTimer">
      Time to vote: {{ votingTimer }} seconds
    </div>
    <div class="timer-text" v-if="store.showBetweenRoundMs">
      Time to next round: {{ betweenRoundsTimer }} seconds
    </div>
  </div>
</template>

<style scoped>
.timer-text {
  font-size: 1.75em;
}
</style>

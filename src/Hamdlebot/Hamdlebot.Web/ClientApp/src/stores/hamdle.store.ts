import { defineStore } from 'pinia';
import { computed, ref } from 'vue';
import { useSignalR } from '@/composables/signalr.composable';

export const useHamdleStore = defineStore('hamdle', () => {
  const { getConnectionByHub } = useSignalR();
  const signalRConnection = getConnectionByHub('hamdlebothub');
  const currentWord = ref('');
  const guesses = ref<string[]>([]);
  const guessMs = ref(0);
  const votingMs = ref(0);
  const betweenRoundMs = ref(0);

  const showGuessTimer = computed(() => guessMs.value > 0);
  const showVotingTimer = computed(() => votingMs.value > 0);
  const showBetweenRoundMs = computed(() => betweenRoundMs.value > 0);

  signalRConnection?.on('startguesstimer', (word: string) => {
    currentWord.value = word;
  });

  signalRConnection?.on('sendguess', (guess: string) => {
    guesses.value.push(guess);
  });

  signalRConnection?.on('resetstate', () => {
    guesses.value = [];
    currentWord.value = '';
  });

  signalRConnection?.on('startguesstimer', (ms) => {
    guessMs.value = ms;
  });

  signalRConnection?.on('startvotetimer', (ms) => {
    votingMs.value = ms;
  });

  signalRConnection?.on('startbetweenroundtimer', (ms) => {
    betweenRoundMs.value = ms;
  });

  function resetGuessTimer(): void {
    guessMs.value = 0;
  }

  function resetVotingTimer(): void {
    votingMs.value = 0;
  }

  function resetBetweenGuessTimer(): void {
    betweenRoundMs.value = 0;
  }

  return {
    currentWord,
    guesses,
    showGuessTimer,
    showVotingTimer,
    showBetweenRoundMs,
    guessMs,
    votingMs,
    betweenRoundMs,
    resetGuessTimer,
    resetVotingTimer,
    resetBetweenGuessTimer
  };
});

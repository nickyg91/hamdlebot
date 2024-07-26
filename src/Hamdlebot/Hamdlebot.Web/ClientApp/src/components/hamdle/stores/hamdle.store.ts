import { defineStore } from 'pinia';
import { computed, ref } from 'vue';
import { useSignalR } from '@/composables/signalr.composable';

export const useHamdleStore = defineStore('hamdle', () => {
  const { createSignalRConnection } = useSignalR();
  const startSignalRConnection = async (twitchUserId: string) => {
    const queryStringParams = new URLSearchParams();
    queryStringParams.append('twitchUserId', twitchUserId);
    createSignalRConnection('hamdlebothub', queryStringParams).then((signalRConnection) => {
      signalRConnection?.on('ReceiveSelectedWord', (word: string) => {
        currentWord.value = word;
      });

      signalRConnection?.on('ReceiveGuess', (guess: string) => {
        guesses.value.push(guess);
      });

      signalRConnection?.on('ReceiveResetState', () => {
        guesses.value = [];
        currentWord.value = '';
      });

      signalRConnection?.on('ReceiveStartGuessTimer', (ms) => {
        guessMs.value = ms;
      });

      signalRConnection?.on('ReceiveStartVoteTimer', (ms) => {
        votingMs.value = ms;
      });

      signalRConnection?.on('ReceiveStartBetweenRoundTimer', (ms) => {
        betweenRoundMs.value = ms;
      });
    });
  };

  const currentWord = ref('');
  const guesses = ref<string[]>([]);
  const guessMs = ref(0);
  const votingMs = ref(0);
  const betweenRoundMs = ref(0);

  const showGuessTimer = computed(() => guessMs.value > 0);
  const showVotingTimer = computed(() => votingMs.value > 0);
  const showBetweenRoundMs = computed(() => betweenRoundMs.value > 0);
  const showConfetti = computed(() => guesses.value.some((x) => x === currentWord.value));

  const resetGuessTimer = (): void => {
    guessMs.value = 0;
  };

  const resetVotingTimer = (): void => {
    votingMs.value = 0;
  };

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
    showConfetti,
    resetGuessTimer,
    resetVotingTimer,
    resetBetweenGuessTimer,
    startSignalRConnection
  };
});

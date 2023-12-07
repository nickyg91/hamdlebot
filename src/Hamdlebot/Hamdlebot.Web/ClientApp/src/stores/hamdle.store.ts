import { defineStore } from 'pinia';
import { computed, ref } from 'vue';
import { HubConnectionBuilder, HubConnection } from '@microsoft/signalr';

export const useHamdleStore = defineStore('hamdle', () => {
  const currentWord = ref('');
  const guesses = ref<string[]>([]);
  const guessMs = ref(0);
  const votingMs = ref(0);
  const betweenRoundMs = ref(0);

  const showGuessTimer = computed(() => guessMs.value > 0);
  const showVotingTimer = computed(() => votingMs.value > 0);
  const showBetweenRoundMs = computed(() => betweenRoundMs.value > 0);

  let signalRConnection: HubConnection;
  async function createSignalRConnection(): Promise<void> {
    const connection = new HubConnectionBuilder().withUrl('/hamdlebothub').build();
    signalRConnection = connection;
    //await startSignalRConnection();
    signalRConnection.on('SendSelectedWord', (word: string) => {
      currentWord.value = word;
    });
    signalRConnection.on('SendGuess', (guess: string) => {
      guesses.value.push(guess);
    });
    signalRConnection.on('ResetState', () => {
      guesses.value = [];
      currentWord.value = '';
    });
    signalRConnection.on('StartGuessTimer', (ms) => {
      guessMs.value = ms;
    });
    signalRConnection.on('StartVoteTimer', (ms) => {
      votingMs.value = ms;
    });
    signalRConnection.on('StartBetweenRoundTimer', (ms) => {
      betweenRoundMs.value = ms;
    });
  }

  async function startSignalRConnection(): Promise<void> {
    signalRConnection.keepAliveIntervalInMilliseconds = 1000;
    await signalRConnection.start();
  }

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
    createSignalRConnection,
    resetGuessTimer,
    resetVotingTimer,
    resetBetweenGuessTimer
  };
});

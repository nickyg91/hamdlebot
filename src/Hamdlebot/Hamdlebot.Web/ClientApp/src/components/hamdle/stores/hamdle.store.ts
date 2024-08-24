import { defineStore } from 'pinia';
import { computed, effect, ref } from 'vue';
import { useSignalR } from '@/composables/signalr.composable';
import { type HubConnection, HubConnectionState } from '@microsoft/signalr';

export const useHamdleStore = defineStore('hamdle', () => {
  const { createSignalRConnection } = useSignalR();
  const signalRConnection = ref<HubConnection | null>(null);
  const msgs = ref<string[]>([]);
  effect(() => {
    console.log(msgs.value);
  });
  const isSignalRConnected = computed(() => {
    if (signalRConnection.value === null) {
      return false;
    }
    return signalRConnection.value.state === HubConnectionState.Connected;
  });
  const startSignalRConnection = async (twitchUserId: string) => {
    const queryStringParams = new URLSearchParams();
    queryStringParams.append('twitchUserId', twitchUserId);
    signalRConnection.value = await createSignalRConnection('hamdlebothub', queryStringParams);

    signalRConnection.value?.on('ReceiveSelectedWord', (word: string) => {
      msgs.value.push('ReceiveSelectedWord');
      currentWord.value = word;
    });

    signalRConnection.value?.on('ReceiveGuess', (guess: string) => {
      msgs.value.push('ReceiveGuess');
      guesses.value.push(guess);
    });

    signalRConnection.value?.on('ReceiveResetState', () => {
      msgs.value.push('ReceiveResetState');
      guesses.value = [];
      currentWord.value = '';
    });

    signalRConnection.value?.on('ReceiveStartGuessTimer', (ms) => {
      msgs.value.push('ReceiveStartGuessTimer');
      guessMs.value = ms;
    });

    signalRConnection.value?.on('ReceiveStartVoteTimer', (ms) => {
      msgs.value.push('ReceiveStartVoteTimer');
      votingMs.value = ms;
    });

    signalRConnection.value?.on('ReceiveStartBetweenRoundTimer', (ms) => {
      msgs.value.push('ReceiveStartBetweenRoundTimer');
      betweenRoundMs.value = ms;
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
    isSignalRConnected,
    msgs,
    resetGuessTimer,
    resetVotingTimer,
    resetBetweenGuessTimer,
    startSignalRConnection
  };
});

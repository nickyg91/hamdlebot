import { defineStore } from 'pinia';
import { ref } from 'vue';
import { HubConnectionBuilder, HubConnection } from '@microsoft/signalr';

export const useHamdleStore = defineStore('hamdle', () => {
  const currentWord = ref('');
  const guesses = ref<string[]>([]);
  let signalRConnection: HubConnection;
  async function createSignalRConnection(): Promise<void> {
    const connection = new HubConnectionBuilder().withUrl('/hamdlebothub').build();
    signalRConnection = connection;
    await startSignalRConnection();
    signalRConnection.on('SendSelectedWord', (word: string) => {
      console.log(word);
      currentWord.value = word;
    });
    signalRConnection.on('SendGuess', (guess: string) => {
      guesses.value.push(guess);
    });
  }

  async function startSignalRConnection(): Promise<void> {
    await signalRConnection.start();
  }

  return {
    currentWord,
    createSignalRConnection
  };
});

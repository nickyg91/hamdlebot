import { useSignalR } from '@/composables/signalr.composable';
import type { ILogMessage } from '@/models/log-message.interface';
import { defineStore } from 'pinia';
import { ref } from 'vue';

export const useDashboardStore = defineStore('dashboard', () => {
  const logMessages = ref<ILogMessage[]>([]);
  const { getConnectionByHub } = useSignalR();
  const signalRConnection = getConnectionByHub('botloghub');

  signalRConnection?.on('logmessage', (message: ILogMessage) => {
    logMessages.value.push(message);
  });

  return {
    logMessages
  };
});

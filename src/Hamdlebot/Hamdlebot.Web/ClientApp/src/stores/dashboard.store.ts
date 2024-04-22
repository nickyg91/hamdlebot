import { useSignalR } from '@/composables/signalr.composable';
import { BotStatusType } from '@/models/bot-status-type.enum';
import type { ILogMessage } from '@/models/log-message.interface';
import { defineStore } from 'pinia';
import { ref } from 'vue';

export const useDashboardStore = defineStore('dashboard', () => {
  const logMessages = ref<ILogMessage[]>([]);
  const botStatus = ref<BotStatusType>(BotStatusType.Offline);

  const { getConnectionByHub } = useSignalR();
  const signalRConnection = getConnectionByHub('botloghub');
  signalRConnection?.on('LogMessage', (message: ILogMessage) => {
    logMessages.value.push(message);
  });

  signalRConnection?.on('SendBotStatus', (status: BotStatusType) => {
    console.log('status', status);
    botStatus.value = status;
  });

  return {
    logMessages,
    botStatus
  };
});

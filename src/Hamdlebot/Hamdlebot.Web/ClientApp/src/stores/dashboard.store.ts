import { useSignalR } from '@/composables/signalr.composable';
import { BotStatusType } from '@/models/bot-status-type.enum';
import type { ILogMessage } from '@/models/log-message.interface';
import { defineStore } from 'pinia';
import { ref } from 'vue';

export const useDashboardStore = defineStore('dashboard', () => {
  const logMessages = ref<ILogMessage[]>([]);
  const botStatus = ref<BotStatusType>(BotStatusType.Offline);

  const { createSignalRConnection } = useSignalR();
  const startDashboardSignalRConnection = async () => {
    createSignalRConnection('botloghub').then((signalRConnection) => {
      signalRConnection?.on('LogMessage', (message: ILogMessage) => {
        logMessages.value.push(message);
      });

      signalRConnection?.on('SendBotStatus', (status: BotStatusType) => {
        if (botStatus.value !== BotStatusType.HamdleInProgress) {
          botStatus.value = status;
        }
      });
    });
  };

  return {
    logMessages,
    botStatus,
    startDashboardSignalRConnection
  };
});

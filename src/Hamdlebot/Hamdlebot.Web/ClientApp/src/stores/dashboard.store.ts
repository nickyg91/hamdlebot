import { useAxios } from '@/composables/http-client.composable';
import { useSignalR } from '@/composables/signalr.composable';
import type { IBotChannel } from '@/models/bot-channel.interface';
import { BotStatusType } from '@/models/bot-status-type.enum';
import type { ILogMessage } from '@/models/log-message.interface';
import { defineStore } from 'pinia';
import { ref } from 'vue';

export const useDashboardStore = defineStore('dashboard', () => {
  const botChannel = ref<IBotChannel | null>(null);
  const logMessages = ref<ILogMessage[]>([]);
  const botStatus = ref<BotStatusType>(BotStatusType.Offline);
  const { httpClient } = useAxios();
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

  const joinChannel = async () => {
    const channel = await httpClient.put<IBotChannel>('/hamdlebot/management/join-channel');
    botChannel.value = channel.data;
  };

  const leaveChannel = async () => {
    await httpClient.put('/hamdlebot/management/leave-channel');
  };

  return {
    logMessages,
    botStatus,
    botChannel,
    startDashboardSignalRConnection,
    joinChannel,
    leaveChannel
  };
});

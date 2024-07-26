import { useBotManagementService } from '@/composables/bot-channel-management.composable';
import { useSignalR } from '@/composables/signalr.composable';
import type { IBotChannel } from '@/models/bot-channel.interface';
import type { IBotChannelCommand } from '@/models/bot-commands.interface';
import { BotStatusType } from '@/models/bot-status-type.enum';
import { ChannelConnectionStatusType } from '@/models/channel-connection-status-type.enum';
import type { ILogMessage } from '@/models/log-message.interface';
import { ObsConnectionStatusType } from '@/models/obs-connection-status-type.enum';
import { defineStore } from 'pinia';
import { ref, watch } from 'vue';

export const useDashboardStore = defineStore('dashboard', () => {
  const botChannel = ref<IBotChannel | null>(null);
  const logMessages = ref<ILogMessage[]>([]);
  const botStatus = ref<BotStatusType>(BotStatusType.Offline);
  const channelConnectionStatus = ref<ChannelConnectionStatusType>(
    ChannelConnectionStatusType.Disconnected
  );
  const obsConnectionStatus = ref<ObsConnectionStatusType>(ObsConnectionStatusType.Disconnected);

  const { getChannel, joinChannel, leaveChannel } = useBotManagementService();
  const { createSignalRConnection } = useSignalR();

  const startDashboardSignalRConnection = async () => {
    createSignalRConnection('botloghub', null).then((signalRConnection) => {
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

  const getMyChannel = async () => {
    const channel = await getChannel();
    botChannel.value = channel;
  };

  const joinMyChannel = async () => {
    const channel = await joinChannel();
    botChannel.value = channel;
  };

  const leaveMyChannel = async () => {
    await leaveChannel();
    botChannel.value = null;
  };

  const addCommandToChannel = (command: IBotChannelCommand) => {
    if (!botChannel.value) {
      return;
    }
    if (!botChannel.value.commands) {
      botChannel.value.commands = [];
    }
    botChannel.value?.commands.push(command);
  };

  const removeCommand = (command: IBotChannelCommand) => {
    if (!botChannel.value) {
      return;
    }
    const index = botChannel.value.commands?.findIndex((c) => c.id === command.id);
    if (index !== -1) {
      botChannel.value.commands?.splice(index, 1);
    }
  };

  const updateCommand = (command: IBotChannelCommand) => {
    if (!botChannel.value) {
      return;
    }
    const index = botChannel.value.commands?.findIndex((c) => c.id === command.id);
    if (index !== -1) {
      botChannel.value.commands[index] = command;
    }
  };

  const updateChannel = (channel: IBotChannel) => {
    botChannel.value = channel;
  };

  watch(
    botChannel,
    (newChannel) => {
      if (newChannel) {
        const queryStringParams = new URLSearchParams();
        queryStringParams.append('twitchUserId', newChannel.twitchUserId);
        createSignalRConnection('channelnotificationshub', queryStringParams).then(
          (signalRConnection) => {
            signalRConnection?.on(
              'ReceiveChannelConnectionStatus',
              (status: ChannelConnectionStatusType) => {
                channelConnectionStatus.value = status;
              }
            );
            signalRConnection?.on(
              'ReceiveObsConnectionStatus',
              (status: ObsConnectionStatusType) => {
                obsConnectionStatus.value = status;
              }
            );
          }
        );
      }
    },
    {
      once: true
    }
  );

  return {
    logMessages,
    botStatus,
    botChannel,
    channelConnectionStatus,
    obsConnectionStatus,
    getMyChannel,
    joinMyChannel,
    leaveMyChannel,
    startDashboardSignalRConnection,
    addCommandToChannel,
    removeCommand,
    updateCommand,
    updateChannel
  };
});

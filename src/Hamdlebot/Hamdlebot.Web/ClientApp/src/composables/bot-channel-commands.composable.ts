import type { IBotChannelCommand } from '@/components/dashboard/models/bot-commands.interface';
import { useAxios } from './http-client.composable';

export const useBotChannelCommands = (channelId: number) => {
  const { httpClient } = useAxios();

  const addChannelCommand = async (command: IBotChannelCommand): Promise<IBotChannelCommand> => {
    const data = (
      await httpClient.post<IBotChannelCommand>(`/channel/${channelId}/commands/add`, command)
    ).data;
    return data;
  };

  const removeChannelCommand = async (commandId: number): Promise<void> => {
    await httpClient.delete(`/channel/${channelId}/commands/remove/${commandId}`);
  };

  const updateChannelCommand = async (command: IBotChannelCommand): Promise<void> => {
    await httpClient.put(`/channel/${channelId}/commands/update`, command);
  };

  return {
    addChannelCommand,
    removeChannelCommand,
    updateChannelCommand
  };
};

import type { IBotChannel } from '@/models/bot-channel.interface';
import { useAxios } from './http-client.composable';

export const useBotManagementService = () => {
  const { httpClient } = useAxios();

  const getChannel = async (): Promise<IBotChannel> => {
    return (await httpClient.get('/hamdlebot/management/channel')).data;
  };

  const joinChannel = async (): Promise<IBotChannel> => {
    const channel = (await httpClient.put<IBotChannel>('/hamdlebot/management/join-channel')).data;
    return channel;
  };

  const leaveChannel = async (): Promise<void> => {
    await httpClient.put('/hamdlebot/management/leave-channel');
  };

  return {
    getChannel,
    joinChannel,
    leaveChannel
  };
};

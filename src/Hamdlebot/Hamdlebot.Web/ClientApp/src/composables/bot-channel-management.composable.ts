import type { IBotChannel } from '@/components/dashboard/models/bot-channel.interface';
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

  const updateHamdleStatus = async (isEnabled: boolean): Promise<IBotChannel> => {
    return (await httpClient.put(`/hamdlebot/management/set-hamdle-optin/${isEnabled}`)).data;
  };

  return {
    getChannel,
    joinChannel,
    leaveChannel,
    updateHamdleStatus
  };
};

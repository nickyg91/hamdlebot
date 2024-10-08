import type { ObsSettings } from '@/components/dashboard/models/obs-settings.model';
import { useAxios } from './http-client.composable';

export const useObsSettingsService = () => {
  const { httpClient } = useAxios();
  const getObsSettings = async () => {
    const { data } = await httpClient.get<ObsSettings>('/obs-settings');
    return data;
  };

  const updateObsSettings = async (settings: ObsSettings) => {
    await httpClient.put('/obs-settings/update', settings);
  };

  const connectToObs = async () => {
    await httpClient.put('/obs-settings/connect');
  };

  const disconnectFromObs = async () => {
    await httpClient.put('/obs-settings/connect');
  };

  return {
    getObsSettings,
    updateObsSettings,
    connectToObs,
    disconnectFromObs
  };
};

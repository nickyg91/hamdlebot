import {
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
  type HubConnection
} from '@microsoft/signalr';
import { computed, ref } from 'vue';
const signalRConnections = ref(new Map<string, HubConnection>());

export const useSignalR = () => {
  const signalRHubStatuses = computed(() => {
    const statuses = new Map<string, HubConnectionState>();
    signalRConnections.value.forEach((connection, hubName) => {
      statuses.set(hubName, connection.state);
    });
    return statuses;
  });
  const createSignalRConnection = async (hubName: string): Promise<void> => {
    if (
      signalRConnections.value.has(hubName) &&
      signalRConnections.value.get(hubName)?.state === HubConnectionState.Connected
    ) {
      return;
    }
    const signalRConnection = new HubConnectionBuilder()
      .withUrl(`/${hubName}`)
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Information)
      .build();

    signalRConnection.keepAliveIntervalInMilliseconds = 1000;

    try {
      await startSignalRConnection(signalRConnection);
    } catch (error) {
      console.error('Error starting signalR connection', error);
    }

    signalRConnections.value.set(hubName, signalRConnection);
  };

  const startSignalRConnection = async (connection: HubConnection): Promise<void> => {
    if (!connection) {
      throw new Error('No signalR connection found');
    }
    connection.keepAliveIntervalInMilliseconds = 1000;
    await connection.start();
  };

  const getConnectionByHub = (hubName: string): HubConnection | null => {
    return (signalRConnections.value.get(hubName) as HubConnection) ?? null;
  };

  const reconnect = () => {
    signalRConnections.value.forEach(async (connection) => {
      if (connection.state === HubConnectionState.Disconnected) {
        await startSignalRConnection(connection as HubConnection);
      }
    });
  };

  return {
    createSignalRConnection,
    getConnectionByHub,
    reconnect,
    signalRHubStatuses
  };
};

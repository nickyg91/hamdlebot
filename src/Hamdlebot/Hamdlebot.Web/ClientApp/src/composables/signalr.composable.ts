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

  const createSignalRConnection = async (
    hubName: string,
    queryStringParams: URLSearchParams | null
  ): Promise<HubConnection> => {
    const existingConnection = signalRConnections.value.get(hubName);
    if (
      signalRConnections.value.has(hubName) &&
      existingConnection?.state === HubConnectionState.Connected
    ) {
      return existingConnection as HubConnection;
    }

    let signalRConnection: HubConnection;
    if ((queryStringParams?.size ?? 0) > 0) {
      const queryString = queryStringParams!.toString();
      signalRConnection = new HubConnectionBuilder()
        .withUrl(`/${hubName}?${queryString}`)
        .withAutomaticReconnect()
        .configureLogging(LogLevel.Debug)
        .build();
    } else {
      signalRConnection = new HubConnectionBuilder()
        .withUrl(`/${hubName}`)
        .withAutomaticReconnect()
        .configureLogging(LogLevel.Debug)
        .build();
    }

    signalRConnection.keepAliveIntervalInMilliseconds = 1000;

    try {
      await startSignalRConnection(signalRConnection);
    } catch (error) {
      console.error('Error starting signalR connection', error);
    }

    signalRConnections.value.set(hubName, signalRConnection);
    return signalRConnection;
  };

  const startSignalRConnection = async (connection: HubConnection): Promise<void> => {
    if (!connection) {
      throw new Error('No signalR connection found');
    }
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

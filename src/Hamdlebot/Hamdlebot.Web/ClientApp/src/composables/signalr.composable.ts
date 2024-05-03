import {
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
  type HubConnection
} from '@microsoft/signalr';
import { useToast } from 'primevue/usetoast';
import { computed, ref } from 'vue';
const signalRConnections = ref(new Map<string, HubConnection>());
export const useSignalR = () => {
  const toast = useToast();
  const signalRHubStatuses = computed(() => {
    const statuses = new Map<string, HubConnectionState>();
    signalRConnections.value.forEach((connection, hubName) => {
      statuses.set(hubName, connection.state);
    });
    return statuses;
  });

  const createSignalRConnection = async (hubName: string): Promise<HubConnection> => {
    const existingConnection = signalRConnections.value.get(hubName);
    if (
      signalRConnections.value.has(hubName) &&
      existingConnection?.state === HubConnectionState.Connected
    ) {
      return existingConnection as HubConnection;
    }
    const signalRConnection = new HubConnectionBuilder()
      .withUrl(`/${hubName}`)
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Information)
      .build();

    signalRConnection.keepAliveIntervalInMilliseconds = 1000;

    try {
      await startSignalRConnection(signalRConnection);
      toast.add({
        closable: true,
        life: 5000,
        severity: 'success',
        summary: `SignalR Hub ${hubName} Connected!`,
        detail: `An error occurred while connecting to the signalR hub ${signalRConnection.baseUrl}.`,
        group: 'signalr'
      });
    } catch (error) {
      toast.add({
        closable: true,
        life: 5000,
        severity: 'error',
        summary: 'SignalR Connection Failure',
        detail: `An error occurred while connecting to the signalR hub ${signalRConnection.baseUrl}.`,
        group: 'signalr'
      });
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

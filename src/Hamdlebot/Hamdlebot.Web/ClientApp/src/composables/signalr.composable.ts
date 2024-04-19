import {
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
  type HubConnection
} from '@microsoft/signalr';
const signalRConnections: Map<string, HubConnection> = new Map<string, HubConnection>();
export const useSignalR = () => {
  async function createSignalRConnection(hubName: string): Promise<HubConnection> {
    if (
      signalRConnections.has(hubName) &&
      signalRConnections.get(hubName)?.state === HubConnectionState.Connected
    ) {
      return signalRConnections.get(hubName)!;
    }
    const signalRConnection = new HubConnectionBuilder()
      .withUrl(`/${hubName}`)
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Information)
      .build();
    signalRConnection.keepAliveIntervalInMilliseconds = 1000;
    await startSignalRConnection(signalRConnection);
    signalRConnections.set(hubName, signalRConnection);
    return signalRConnection;
  }

  async function startSignalRConnection(connection: HubConnection): Promise<void> {
    if (!connection) {
      throw new Error('No signalR connection found');
    }
    connection.keepAliveIntervalInMilliseconds = 1000;
    await connection.start();
  }

  const getConnectionByHub = (hubName: string): HubConnection | null => {
    return signalRConnections.get(hubName) ?? null;
  };

  return {
    createSignalRConnection,
    getConnectionByHub,
    signalRConnections
  };
};

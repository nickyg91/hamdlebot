import type { HubConnectionState } from '@microsoft/signalr';

export interface IHubStatus {
  connection: HubConnectionState;
}

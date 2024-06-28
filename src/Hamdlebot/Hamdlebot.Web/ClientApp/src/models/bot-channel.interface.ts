import type { IBotChannelCommand } from './bot-commands.interface';

export interface IBotChannel {
  id: number;
  twitchUserId: string;
  twitchUserName: string;
  isHamdleEnabled: boolean;
  allowAccessToObs: boolean;
  commands: IBotChannelCommand[];
}

import type { SeverityLevel } from './severity-level.enum';

export interface ILogMessage {
  message: string;
  timeStamp: Date;
  severityLevel: SeverityLevel;
}

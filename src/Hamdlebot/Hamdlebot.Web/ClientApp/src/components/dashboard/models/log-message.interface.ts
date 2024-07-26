import type { SeverityLevel } from './severity-level.enum';

export interface ILogMessage {
  message: string;
  timestamp: string;
  severityLevel: SeverityLevel;
}

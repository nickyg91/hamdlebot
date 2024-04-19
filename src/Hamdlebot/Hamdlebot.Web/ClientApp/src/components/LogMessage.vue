<script setup lang="ts">
import type { ILogMessage } from '@/models/log-message.interface';
import { SeverityLevel } from '@/models/severity-level.enum';
defineProps<{ message: ILogMessage }>();

const toTimestamp = (date: Date) => date.toISOString().replace('T', ' ').replace('Z', '');
const toLevel = (level: SeverityLevel) => SeverityLevel[level];
</script>
<template>
  <div class="log-message">
    <span>{{ toTimestamp(message.timeStamp) }}</span>
    <span>{{ toLevel(message.severityLevel) }}</span>
    <span>{{ message.message }}</span>
  </div>
</template>

<style scoped>
.log-message {
  font-family: 'Courier New', Courier, monospace;
}
</style>

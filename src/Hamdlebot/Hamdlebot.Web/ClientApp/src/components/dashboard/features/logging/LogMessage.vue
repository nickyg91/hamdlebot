<script setup lang="ts">
import { SeverityLevel } from '@/components/dashboard/models/severity-level.enum';
import type { ILogMessage } from '@/components/dashboard/models/log-message.interface';

defineProps<{ message: ILogMessage }>();

const toTimestamp = (date: string) => date?.replace('T', ' ')?.replace('Z', '') + ' UTC';
const toLevel = (level: SeverityLevel) => SeverityLevel[level];
</script>
<template>
  <div class="log-message p-2">
    <span class="mr-2">{{ toTimestamp(message.timestamp) }}</span>
    <span
      :class="{
        'text-blue-500': message.severityLevel === SeverityLevel.Info,
        'text-red-500': message.severityLevel === SeverityLevel.Error,
        'text-yellow-500': message.severityLevel === SeverityLevel.Warning
      }"
      class="mr-2"
      >{{ toLevel(message.severityLevel) }}</span
    >
    <span class="mr-2">{{ message.message }}</span>
  </div>
</template>

<style scoped>
.log-message {
  font-family: 'Courier New', Courier, monospace;
}
</style>

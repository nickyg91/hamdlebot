<script setup lang="ts">
import LogMessage from '@/components/LogMessage.vue';
import { useDashboardStore } from '@/stores/dashboard.store';
import Panel from 'primevue/panel';

const dashboardStore = useDashboardStore();
</script>
<template>
  <div>
    <Panel header="Bot Log">
      <LogMessage
        class="p-2"
        v-for="message in dashboardStore.logMessages"
        :key="message.timestamp"
        :message="message"
      />
    </Panel>
  </div>
</template>
<style scoped></style>

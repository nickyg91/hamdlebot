<script setup lang="ts">
import LogMessage from '@/components/LogMessage.vue';
import { useSignalR } from '@/composables/signalr.composable';
import { useDashboardStore } from '@/stores/dashboard.store';
import Panel from 'primevue/panel';
import { onMounted } from 'vue';
const { createSignalRConnection } = useSignalR();
onMounted(async () => {
  await createSignalRConnection('botloghub');
});

const dashboardStore = useDashboardStore();
</script>
<template>
  <div>
    <Panel header="Bot Log">
      <LogMessage
        class="p-2"
        v-for="message in dashboardStore.logMessages"
        :key="message.timeStamp.toISOString()"
        :message="message"
      />
    </Panel>
  </div>
</template>
<style scoped></style>

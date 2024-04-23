<script setup lang="ts">
import LogMessage from '@/components/LogMessage.vue';
import InlineMessage from 'primevue/inlinemessage';
import { useDashboardStore } from '@/stores/dashboard.store';
import ScrollPanel from 'primevue/scrollpanel';
import Panel from 'primevue/panel';
import Button from 'primevue/button';
import { BotStatusType } from '@/models/bot-status-type.enum';
import { computed } from 'vue';
import { useTwitchAuthService } from '@/composables/twitch-auth.composable';
import { useSignalR } from '@/composables/signalr.composable';
import { HubConnectionState } from '@microsoft/signalr';
import { storeToRefs } from 'pinia';
import { useHamdleStore } from '@/stores/hamdle.store';

const store = useDashboardStore();
const hamdleStore = useHamdleStore();
const { currentWord, guesses } = storeToRefs(hamdleStore);
const { botStatus, logMessages } = storeToRefs(store);
const { reconnect, signalRHubStatuses } = useSignalR();
const botStatusSeverity = computed(() => {
  switch (botStatus.value) {
    case BotStatusType.Online:
      return {
        class: 'success',
        text: 'Online'
      };
    case BotStatusType.Offline:
      return {
        class: 'error',
        text: 'Offline'
      };
    case BotStatusType.HamdleInProgress:
      return {
        class: 'info',
        text: 'Hamdle Game In Progress'
      };
    default:
      return {
        class: 'error',
        text: 'Offline'
      };
  }
});

const twitchAuthService = useTwitchAuthService();
const getAuthUrl = async () => {
  const authUrl = await twitchAuthService.getTwitchAuthUrl();
  window.open(authUrl, '_blank');
};

const hubs = computed(() => {
  const signalRHubs: { hubName: string; status: HubConnectionState }[] = [];
  signalRHubStatuses.value.forEach((state, name) => {
    signalRHubs.push({
      hubName: name,
      status: state
    });
  });
  return signalRHubs;
});
</script>
<template>
  <div class="flex justify-content-between">
    <div class="flex-grow-1 p-2">
      <Panel>
        <template #header>
          <h2>
            Bot Status
            <InlineMessage class="ml-3" :severity="botStatusSeverity.class">
              {{ botStatusSeverity.text }}
            </InlineMessage>
          </h2>
        </template>
        <div>
          <div>
            <Button severity="help" label="Authenticate" @click="getAuthUrl"></Button>
          </div>
        </div>
        <hr />
        <div class="mt-2">
          <h3>SignalR Connections</h3>
          <Button label="Reconnect" @click="reconnect"></Button>
          <div class="mt-2" v-for="hub in hubs" :key="hub.hubName">
            <InlineMessage
              :severity="hub.status === HubConnectionState.Connected ? 'success' : 'error'"
            >
              {{ hub.hubName }} - {{ hub.status }}
            </InlineMessage>
          </div>
        </div>
        <div v-if="botStatus === BotStatusType.HamdleInProgress" class="mt-2">
          <h3>Hamdle Status</h3>
          <div>Current word: {{ currentWord }}</div>
          <ul>
            <li v-for="guess in guesses" :key="guess">
              {{ guess }}
            </li>
          </ul>
        </div>
      </Panel>
    </div>
    <div class="flex-grow-1 p-2">
      <Panel>
        <template #header>
          <h2>Bot Log</h2>
        </template>
        <ScrollPanel class="scroll-panel">
          <LogMessage
            class="p-2"
            v-for="message in logMessages"
            :key="message.timestamp"
            :message="message"
          />
        </ScrollPanel>
      </Panel>
    </div>
  </div>
</template>
<style scoped>
.scroll-panel {
  height: 450px;
  width: 100%;
}
</style>

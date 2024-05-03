<script setup lang="ts">
import LogMessage from '@/components/LogMessage.vue';
import InlineMessage from 'primevue/inlinemessage';
import { useDashboardStore } from '@/stores/dashboard.store';
import ScrollPanel from 'primevue/scrollpanel';
import Panel from 'primevue/panel';
import Button from 'primevue/button';
import Dialog from 'primevue/dialog';
import Sidebar from 'primevue/sidebar';
import ObsSettings from '@/components/ObsSettings.vue';
import { BotStatusType } from '@/models/bot-status-type.enum';
import { computed, ref } from 'vue';
import { useSignalR } from '@/composables/signalr.composable';
import { HubConnectionState } from '@microsoft/signalr';
import { storeToRefs } from 'pinia';
import { useHamdleStore } from '@/stores/hamdle.store';
import { useAuthStore } from '@/stores/auth.store';
import { watch } from 'vue';

const store = useDashboardStore();
const hamdleStore = useHamdleStore();
const authStore = useAuthStore();
const { token } = storeToRefs(authStore);
const { currentWord, guesses } = storeToRefs(hamdleStore);
const { botStatus, logMessages } = storeToRefs(store);
const { reconnect, signalRHubStatuses } = useSignalR();

const isLoginDialogOpen = ref(false);
const isObsSettingsSliderOpen = ref(false);

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

const isHamdleRunning = computed(() => botStatus.value === BotStatusType.HamdleInProgress);

watch(
  token,
  async () => {
    if (!authStore.token) {
      isLoginDialogOpen.value = true;
    } else {
      await store.startDashboardSignalRConnection();
    }
  },
  { immediate: true }
);

const getAuthUrl = async () => {
  try {
    const authUrl = await authStore.getTwitchAuthUrl();
    window.open(authUrl, 'newwin', 'height=450px,width=450px');
  } catch (error) {
    console.error(error);
  }
};

const getTwitchOAuthUrl = async () => {
  try {
    const authUrl = await authStore.getTwitchOIDCUrl();
    window.open(authUrl, '_self');
  } catch (error) {
    console.error(error);
  }
};
</script>
<template>
  <div>
    <div v-if="authStore.token">
      <section class="flex justify-content-between">
        <div class="flex-grow-1 p-2">
          <Sidebar
            v-model:visible="isObsSettingsSliderOpen"
            header="OBS Settings"
            class="w-full md:w-20rem lg:w-30rem"
          >
            <ObsSettings @on-update-suceeded="isObsSettingsSliderOpen = false"></ObsSettings>
          </Sidebar>
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
                <Button
                  severity="help"
                  icon="pi pi-verified"
                  label="Authenticate Bot"
                  @click="getAuthUrl"
                ></Button>
                <Button
                  class="ml-3"
                  severity="info"
                  label="Obs Settings"
                  icon="pi pi-cog"
                  @click="isObsSettingsSliderOpen = true"
                >
                </Button>
              </div>
            </div>
            <hr />
            <div class="mt-2">
              <h3>SignalR Connections</h3>
              <Button label="Reconnect Hubs" icon="pi pi-wifi" @click="reconnect"></Button>
              <div class="mt-2" v-for="hub in hubs" :key="hub.hubName">
                <InlineMessage
                  :severity="hub.status === HubConnectionState.Connected ? 'success' : 'error'"
                >
                  {{ hub.hubName }} - {{ hub.status }}
                </InlineMessage>
              </div>
            </div>
          </Panel>
        </div>
        <div class="flex-grow-1 p-2">
          <Panel>
            <template #header>
              <h2>Hamdle</h2>
            </template>
            <div>
              <InlineMessage :severity="isHamdleRunning ? 'success' : 'error'">
                Hamdle Session {{ isHamdleRunning ? 'is running' : 'is not running' }}
              </InlineMessage>
            </div>
            <div class="p-2 mt-2" v-if="isHamdleRunning && currentWord">
              Current word: {{ currentWord }}
            </div>
            <template v-if="isHamdleRunning">
              <div class="p-2 mt-2" v-for="guess in guesses" :key="guess">
                {{ guess }}
              </div>
            </template>
          </Panel>
        </div>
      </section>
      <section>
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
      </section>
    </div>
    <Dialog
      v-model:visible="isLoginDialogOpen"
      modal
      header="Log In To Twitch"
      :closable="false"
      :style="{ width: '25rem' }"
    >
      <div class="flex justify-content-center mt-2">
        <Button
          label="Log In"
          icon="pi pi-twitch"
          @click="getTwitchOAuthUrl"
          severity="help"
        ></Button>
      </div>
    </Dialog>
  </div>
</template>
<style scoped>
.scroll-panel {
  height: 450px;
  width: 100%;
}
</style>

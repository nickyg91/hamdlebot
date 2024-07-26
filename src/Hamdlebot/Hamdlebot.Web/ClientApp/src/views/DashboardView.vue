<script setup lang="ts">
import InlineMessage from 'primevue/inlinemessage';
import { useDashboardStore } from '@/components/dashboard/stores/dashboard.store';
import Panel from 'primevue/panel';
import Button from 'primevue/button';
import Dialog from 'primevue/dialog';
import Sidebar from 'primevue/sidebar';
import ObsSettingsSlider from '@/components/dashboard/features/obs/ObsSettingsSlider.vue';
import { BotStatusType } from '@/components/dashboard/models/bot-status-type.enum';
import { computed, ref } from 'vue';
import { storeToRefs } from 'pinia';
import { useAuthStore } from '@/stores/auth.store';
import { watch } from 'vue';
import { useConfirm } from 'primevue/useconfirm';
import ConfirmDialog from 'primevue/confirmdialog';
import ChannelCommands from '@/components/dashboard/features/commands/ChannelCommands.vue';
import ScrollPanel from 'primevue/scrollpanel';
import LogMessage from '@/components/dashboard/features/log/LogMessage.vue';
import type { ObsSettings } from '@/components/dashboard/models/obs-settings.model';
import { useObsSettingsService } from '@/composables/obs-settings.composable';
import HamdleSettings from '@/components/dashboard/features/channel-settings/HamdleSettings.vue';
import ChannelConnectionStatus from '@/components/dashboard/features/channel-settings/ChannelConnectionStatus.vue';

const dashboardStore = useDashboardStore();
const { connectToObs, disconnectFromObs } = useObsSettingsService();
const { botChannel } = storeToRefs(dashboardStore);
const authStore = useAuthStore();
const { token } = storeToRefs(authStore);
const confirm = useConfirm();
const obsSettings = ref<ObsSettings | null>(null);
const isLoginDialogOpen = ref(false);
const isObsSettingsSliderOpen = ref(false);
const botStatusSeverity = computed(() => {
  switch (dashboardStore.botStatus) {
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

watch(
  token,
  async () => {
    if (!authStore.token) {
      isLoginDialogOpen.value = true;
    } else {
      if (authStore.isHamdlebot) {
        await dashboardStore.startDashboardSignalRConnection();
      }
      try {
        await dashboardStore.getMyChannel();
      } catch (error) {
        console.error(error);
      }
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

const showWarningModal = () => {
  confirm.require({
    message:
      'There may be sensitive information in this form. Make sure you are not showing this on stream!',
    header: 'Warning! Sensitive Information!',
    icon: 'pi pi-exclamation-triangle',
    rejectClass: 'p-button-secondary p-button-outlined',
    rejectLabel: 'Cancel',
    acceptLabel: 'Ok',
    accept: () => {
      isObsSettingsSliderOpen.value = true;
    }
  });
};

const joinChannel = async () => {
  try {
    await dashboardStore.joinMyChannel();
  } catch (error) {
    console.error(error);
  }
};

const leaveChannel = async () => {
  try {
    await dashboardStore.leaveMyChannel();
  } catch (error) {
    console.error(error);
  }
};

const onObsUpdateSucceeded = (settings: ObsSettings) => {
  obsSettings.value = settings;
  isObsSettingsSliderOpen.value = false;
};

const onConnectToObs = async () => {
  await connectToObs();
};

const onDisconnectFromObs = async () => {
  await disconnectFromObs();
};
</script>
<template>
  <div class="min-h-screen">
    <div v-if="authStore.token && authStore.jwtDecoded">
      <section class="flex justify-content-between min-h-screen">
        <div class="flex-grow-1 p-2">
          <Sidebar
            v-model:visible="isObsSettingsSliderOpen"
            header="OBS Settings"
            class="w-full md:w-20rem lg:w-30rem"
          >
            <ObsSettingsSlider @on-update-suceeded="onObsUpdateSucceeded"></ObsSettingsSlider>
          </Sidebar>
          <section class="mt-2">
            <div v-if="authStore.isHamdlebot">
              <h2>
                Bot Status
                <InlineMessage class="ml-3" :severity="botStatusSeverity.class">
                  {{ botStatusSeverity.text }}
                </InlineMessage>
              </h2>
            </div>
            <div v-else>
              <h2>
                Welcome to your Hamdlebot Dashboard, {{ authStore.jwtDecoded.preferred_username }}!
              </h2>
            </div>
          </section>
          <section class="mt-2">
            <div class="grid grid-nogutter gap-3">
              <div v-if="botChannel" class="col">
                <Panel>
                  <template #header>
                    <h2>Hamdle Settings</h2>
                  </template>
                  <div>
                    <HamdleSettings
                      :twitch-user-id="authStore.jwtDecoded.sub"
                      :channel="botChannel"
                    ></HamdleSettings>
                  </div>
                </Panel>
              </div>
              <div class="col">
                <Panel>
                  <template #header>
                    <h2>Channel Actions</h2>
                  </template>
                  <ChannelConnectionStatus
                    :bot-channel-connection-status="dashboardStore.channelConnectionStatus"
                    :obs-connection-status="dashboardStore.obsConnectionStatus"
                  />
                  <div class="flex justify-content-evenly mt-5">
                    <Button
                      v-if="authStore.isHamdlebot"
                      severity="help"
                      size="small"
                      icon="pi pi-verified"
                      label="Authenticate Bot"
                      @click="getAuthUrl"
                    ></Button>
                    <Button
                      v-if="!authStore.isHamdlebot && botChannel?.isHamdleEnabled"
                      class="ml-3"
                      size="small"
                      severity="info"
                      label="Obs Settings"
                      icon="pi pi-cog"
                      @click="showWarningModal()"
                    >
                    </Button>
                    <Button
                      class="ml-3"
                      size="small"
                      severity="success"
                      icon="pi pi-user-plus"
                      label="Join Channel"
                      @click="joinChannel"
                    ></Button>
                    <Button
                      class="ml-3"
                      size="small"
                      severity="danger"
                      icon="pi pi-user-minus"
                      label="Leave Channel"
                      @click="leaveChannel"
                    ></Button>
                    <template
                      v-if="
                        !authStore.isHamdlebot &&
                        obsSettings &&
                        botChannel?.allowAccessToObs &&
                        botChannel?.isHamdleEnabled
                      "
                    >
                      <div class="flex mt-2">
                        <Button
                          class="ml-3"
                          size="small"
                          severity="contrast"
                          icon="pi pi-sign-in"
                          label="Connect to OBS"
                          @click="onConnectToObs"
                        >
                        </Button>
                        <Button
                          class="ml-3"
                          size="small"
                          severity="warning"
                          icon="pi pi-sign-out"
                          label="Disconnect from OBS"
                          @click="onDisconnectFromObs"
                        >
                        </Button>
                      </div>
                    </template>
                  </div>
                  <ChannelCommands
                    v-if="botChannel"
                    :channel-id="botChannel!.id"
                    :commands="botChannel!.commands"
                  ></ChannelCommands>
                </Panel>
              </div>
            </div>
          </section>
          <section class="mt-4" v-if="authStore.isHamdlebot">
            <Panel>
              <div class="flex-grow-1 p-2">
                <Panel>
                  <template #header>
                    <h2>Bot Log</h2>
                  </template>
                  <ScrollPanel class="scroll-panel">
                    <LogMessage
                      class="p-2"
                      v-for="message in dashboardStore.logMessages"
                      :key="message.timestamp"
                      :message="message"
                    />
                  </ScrollPanel>
                </Panel>
              </div>
            </Panel>
          </section>
        </div>
      </section>
    </div>
    <Dialog
      v-model:visible="isLoginDialogOpen"
      modal
      :draggable="false"
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
    <ConfirmDialog></ConfirmDialog>
  </div>
</template>
<style scoped>
.scroll-panel {
  height: 450px;
  width: 100%;
}
</style>

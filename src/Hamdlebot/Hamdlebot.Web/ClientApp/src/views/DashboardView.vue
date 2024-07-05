<script setup lang="ts">
import InlineMessage from 'primevue/inlinemessage';
import { useDashboardStore } from '@/stores/dashboard.store';
import Panel from 'primevue/panel';
import Button from 'primevue/button';
import Dialog from 'primevue/dialog';
import Sidebar from 'primevue/sidebar';
import ObsSettings from '@/components/ObsSettings.vue';
import { BotStatusType } from '@/models/bot-status-type.enum';
import { computed, ref } from 'vue';
import { storeToRefs } from 'pinia';
import { useAuthStore } from '@/stores/auth.store';
import { watch } from 'vue';
import { useConfirm } from 'primevue/useconfirm';
import ConfirmDialog from 'primevue/confirmdialog';
import ChannelCommands from '@/components/dashboard/ChannelCommands.vue';
import ScrollPanel from 'primevue/scrollpanel';
import LogMessage from '@/components/LogMessage.vue';

const dashboardStore = useDashboardStore();
const { botChannel } = storeToRefs(dashboardStore);
const authStore = useAuthStore();
const { token } = storeToRefs(authStore);
const confirm = useConfirm();
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
</script>
<template>
  <div class="min-h-screen">
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
          <Panel class="min-h-screen">
            <template v-if="authStore.isHamdlebot" #header>
              <h2>
                Bot Status
                <InlineMessage class="ml-3" :severity="botStatusSeverity.class">
                  {{ botStatusSeverity.text }}
                </InlineMessage>
              </h2>
            </template>
            <template v-else #header>
              <h2>
                Welcome to your Hamdlebot Dashboard, {{ authStore.jwtDecoded?.preferred_username }}!
              </h2>
            </template>
            <div>
              <Button
                v-if="authStore.isHamdlebot"
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
                @click="showWarningModal()"
              >
              </Button>
              <Button
                class="ml-3"
                severity="success"
                icon="pi pi-user-plus"
                label="Join Channel"
                @click="joinChannel"
              ></Button>
              <Button
                class="ml-3"
                severity="danger"
                icon="pi pi-user-minus"
                label="Leave Channel"
                @click="leaveChannel"
              ></Button>
            </div>
            <hr />
            <div v-if="botChannel" class="grid grid-nogutter">
              <div class="col-6">
                <ChannelCommands
                  :channel-id="botChannel!.id"
                  :commands="botChannel!.commands"
                ></ChannelCommands>
              </div>
            </div>
            <section>
              <div v-if="authStore.isHamdlebot" class="flex-grow-1 p-2">
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
            </section>
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
    <ConfirmDialog></ConfirmDialog>
  </div>
</template>
<style scoped>
.scroll-panel {
  height: 450px;
  width: 100%;
}
</style>

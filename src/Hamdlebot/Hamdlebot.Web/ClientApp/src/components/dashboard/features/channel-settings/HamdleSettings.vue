<script setup lang="ts">
import { useBotManagementService } from '@/composables/bot-channel-management.composable';
import { useDashboardStore } from '@/components/dashboard/stores/dashboard.store';
import Checkbox from 'primevue/checkbox';
import { computed, ref } from 'vue';

const props = defineProps<{ twitchUserId: string }>();

const dashboardStore = useDashboardStore();

const botManagementService = useBotManagementService();

const hamdleSourceUrl = computed(() => {
  return `${import.meta.env.VITE_BASE_OBS_LAYOUT_URL}/${props.twitchUserId}/hamdle`;
});

const isHamdleEnabled = ref(dashboardStore.botChannel!.isHamdleEnabled);

const updateHamdleSettings = async (value: boolean) => {
  try {
    const updatedChannel = await botManagementService.updateHamdleStatus(value);
    dashboardStore.updateChannel(updatedChannel);
  } catch (error) {
    console.error(error);
  }
};
</script>
<template>
  <section>
    <div>
      <h3>What is Hamdle?</h3>
      <p>
        Hamdle is a version of wordle that you can play with chat. In order to utilize this feature
        you must opt in using the checkbox below. This feature requires usage of OBS to function.
        Please follow the instructions on enabling OBS web sockets
        <a target="_blank" href="https://obsproject.com/kb/remote-control-guide">here.</a>
      </p>
      <p>
        You must then also enable port forwarding on your router to the port that OBS runs on
        (default is 4455). Please follow your router's instructions on how to do this. The URL
        should look like
        <strong>ws://your-ip:port-number</strong> (ws://1.2.3.4:4455).
      </p>
    </div>
    <div>
      <h1>Hamdle Settings</h1>
      <p>Hamdle Browser Source URL: {{ hamdleSourceUrl }}</p>
    </div>
    <div>
      <label for="hamdleEnabled">
        Enable Hamdle
        <Checkbox
          id="hamdleEnabled"
          @update:model-value="updateHamdleSettings"
          v-model="isHamdleEnabled"
          :binary="true"
        />
      </label>
    </div>
  </section>
</template>

<style scoped></style>

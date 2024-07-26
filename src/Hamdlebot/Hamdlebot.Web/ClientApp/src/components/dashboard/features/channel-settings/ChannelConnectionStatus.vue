<script setup lang="ts">
import { ChannelConnectionStatusType } from '@/components/dashboard/models/channel-connection-status-type.enum';
import { ObsConnectionStatusType } from '@/components/dashboard/models/obs-connection-status-type.enum';
import { computed } from 'vue';
import ConnectionBadge from '@/components/ui/ConnectionBadge.vue';

const props = defineProps<{
  botChannelConnectionStatus: ChannelConnectionStatusType;
  obsConnectionStatus: ObsConnectionStatusType;
}>();

const connected = '#15cc1a';
const disconnected = '#8b948c';
const errored = '#f70000';

const channelConnectionColor = computed(() => {
  switch (props.botChannelConnectionStatus) {
    case ChannelConnectionStatusType.Connected:
      return connected;
    case ChannelConnectionStatusType.Disconnected:
      return disconnected;
    case ChannelConnectionStatusType.Errored:
      return errored;
    default:
      return disconnected;
  }
});

const obsConnectionColor = computed(() => {
  switch (props.obsConnectionStatus) {
    case ObsConnectionStatusType.Connected:
      return connected;
    case ObsConnectionStatusType.Disconnected:
      return disconnected;
    case ObsConnectionStatusType.Errored:
      return errored;
    default:
      return disconnected;
  }
});
</script>
<template>
  <div class="flex justify-content-evenly">
    <ConnectionBadge label="Bot Connection" :size="'medium'" :color="channelConnectionColor" />
    <ConnectionBadge label="OBS Connection" :size="'medium'" :color="obsConnectionColor" />
  </div>
</template>

<style scoped></style>

<script setup lang="ts">
import { useObsSettingsService } from '@/composables/obs-settings.composable';
import type { ObsSettings } from '@/models/obs-settings.model';
import { onMounted } from 'vue';
import { ref } from 'vue';
import ObsSettingsForm from './ObsSettingsForm.vue';
const emits = defineEmits<{ (e: 'onUpdateSuceeded'): void }>();
const { getObsSettings, updateObsSettings } = useObsSettingsService();
const obsSettings = ref<ObsSettings | null>(null);
const isLoading = ref(false);
onMounted(async () => {
  isLoading.value = true;
  try {
    obsSettings.value = await getObsSettings();
  } catch (error) {
    console.error(error);
  } finally {
    isLoading.value = false;
  }
});

const onUpdateObsSettings = async (obsSettings: ObsSettings) => {
  isLoading.value = true;
  try {
    await updateObsSettings(obsSettings);
    emits('onUpdateSuceeded');
  } catch (error) {
    console.error(error);
  } finally {
    isLoading.value = false;
  }
};
</script>

<template>
  <div>
    <Suspense>
      <div v-if="obsSettings && !isLoading">
        <ObsSettingsForm :obsSettings="obsSettings" @update:obsSettings="onUpdateObsSettings" />
      </div>
    </Suspense>
  </div>
</template>

<style scoped></style>

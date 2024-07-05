<script setup lang="ts">
import { useObsSettingsService } from '@/composables/obs-settings.composable';
import { ObsSettings } from '@/models/obs-settings.model';
import { onMounted } from 'vue';
import { ref } from 'vue';
import ObsSettingsForm from './ObsSettingsForm.vue';
const emits = defineEmits<{ (e: 'onUpdateSuceeded', settings: ObsSettings): void }>();
const { getObsSettings, updateObsSettings } = useObsSettingsService();
const obsSettings = ref<ObsSettings>(new ObsSettings());
const isLoading = ref(false);
onMounted(async () => {
  isLoading.value = true;
  try {
    const settings = await getObsSettings();
    if (settings) {
      obsSettings.value = settings;
    }
  } catch (error) {
    console.error(error);
  } finally {
    isLoading.value = false;
  }
});

const onUpdateObsSettings = async (updatedObsSettings: ObsSettings) => {
  isLoading.value = true;
  try {
    await updateObsSettings(updatedObsSettings);
    emits('onUpdateSuceeded', updatedObsSettings);
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
      <div v-if="!isLoading">
        <ObsSettingsForm :obsSettings="obsSettings" @update:obsSettings="onUpdateObsSettings" />
      </div>
    </Suspense>
  </div>
</template>

<style scoped></style>

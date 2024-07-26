<script setup lang="ts">
import type { ObsSettings } from '@/components/dashboard/models/obs-settings.model';
import { toTypedSchema } from '@vee-validate/zod';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Password from 'primevue/password';
import Checkbox from 'primevue/checkbox';
import { useForm } from 'vee-validate';
import * as zod from 'zod';
const props = defineProps<{ obsSettings: ObsSettings }>();
const emits = defineEmits<{ (e: 'update:obsSettings', obsSettings: ObsSettings): void }>();

const schema = toTypedSchema(
  zod.object({
    socketUrl: zod
      .string({
        message: 'Socket URL is required.'
      })
      .min(1),
    hamdleSourceName: zod
      .string({
        message: 'Hamdle Source Name is required.'
      })
      .min(1),
    sceneName: zod
      .string({
        message: 'Scene Name is required.'
      })
      .min(1),
    obsAuthentication: zod
      .string({
        message: 'OBS Websocket Password is required.'
      })
      .min(1),
    isObsEnabled: zod.boolean()
  })
);
const { errors, defineField, handleSubmit, meta } = useForm({
  validationSchema: schema,
  initialValues: props.obsSettings
});

const onSubmit = handleSubmit((values) => {
  if (!meta.value.valid) {
    return;
  }
  emits('update:obsSettings', values as ObsSettings);
});

const [socketUrl] = defineField('socketUrl');
const [hamdleSourceName] = defineField('hamdleSourceName');
const [sceneName] = defineField('sceneName');
const [obsAuthentication] = defineField('obsAuthentication');
const [isObsEnabled] = defineField('isObsEnabled');
</script>
<template>
  <div>
    <form novalidate>
      <div class="field">
        <label for="socketUrl">Socket URL</label>
        <div>
          <Password
            :feedback="false"
            toggleMask
            pt:root:class="w-full"
            pt:input:root:class="w-full"
            v-model="socketUrl"
            :class="{ 'p-invalid': errors.socketUrl }"
            id="socketUrl"
          />
        </div>
      </div>
      <div class="field">
        <label for="hamdleSourceName">Hamdle Source Name</label>
        <div>
          <InputText
            pt:root:class="w-full"
            v-model="hamdleSourceName"
            id="hamdleSourceName"
            :class="{ 'p-invalid': errors.hamdleSourceName }"
          />
        </div>
      </div>
      <div class="field">
        <label for="sceneName">OBS Scene Name (should contain the Hamdle Source Name)</label>
        <div>
          <InputText
            pt:root:class="w-full"
            v-model="sceneName"
            id="sceneName"
            :class="{ 'p-invalid': errors.sceneName }"
          />
        </div>
      </div>
      <div class="field">
        <label for="obsAuthentication">OBS Websocket Password</label>
        <div>
          <Password
            pt:root:class="w-full"
            pt:input:root:class="w-full"
            toggleMask
            v-model="obsAuthentication"
            id="obsAuthentication"
            :class="{ 'p-invalid': errors.obsAuthentication }"
          />
        </div>
      </div>
      <div class="field">
        <label for="isObsEnabled">Enable OBS</label>
        <div>
          <Checkbox v-model="isObsEnabled" :binary="true" />
        </div>
      </div>
      <div class="mt-3">
        <Button
          :disabled="!meta.valid"
          class="w-full"
          label="Save"
          icon="pi pi-save"
          severity="success"
          @click="onSubmit"
        >
        </Button>
      </div>
    </form>
  </div>
</template>

<style scoped></style>

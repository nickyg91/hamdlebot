<script setup lang="ts">
import type { IBotChannelCommand } from '@/components/dashboard/models/bot-commands.interface';
import { toTypedSchema } from '@vee-validate/zod';
import { useForm } from 'vee-validate';
import InputText from 'primevue/inputtext';
import zod from 'zod';
import Button from 'primevue/button';
import Textarea from 'primevue/textarea';
import { useBotChannelCommands } from '@/composables/bot-channel-commands.composable';
import { ref } from 'vue';
import ProgressSpinner from 'primevue/progressspinner';

const props = defineProps<{
  command: IBotChannelCommand | null;
  channelId: number;
}>();

const emits = defineEmits<{
  (e: 'submit:command', command: IBotChannelCommand): void;
  (e: 'cancel:command'): void;
}>();

const { addChannelCommand, updateChannelCommand } = useBotChannelCommands(props.channelId);

const isLoading = ref(false);

const schema = toTypedSchema(
  zod.object({
    command: zod
      .string({
        message: 'Command is required.'
      })
      .min(1)
      .max(64),
    response: zod
      .string({
        message: 'Response is required.'
      })
      .min(1)
      .max(1024)
  })
);

const { errors, defineField, handleSubmit, meta } = useForm({
  validationSchema: schema,
  initialValues: props.command
});

const onCancel = () => {
  emits('cancel:command');
};

const onSubmit = handleSubmit(async (values) => {
  if (!meta.value.valid) {
    return;
  }
  const value = values as IBotChannelCommand;
  if (props.command?.id) {
    value.id = props.command.id;
  }
  value.botChannelId = props.channelId;
  isLoading.value = true;
  try {
    if (props.command?.id) {
      await updateChannelCommand(value);
    } else {
      await addChannelCommand(value);
    }
    emits('submit:command', values as IBotChannelCommand);
  } catch (error) {
    console.error(error);
  } finally {
    isLoading.value = false;
  }
});

const [formCommand] = defineField('command');
const [response] = defineField('response');
</script>
<template>
  <Suspense>
    <div class="flex justify-content-center">
      <ProgressSpinner v-if="isLoading" />
      <form v-if="!isLoading" novalidate>
        <div class="field">
          <label for="command">Command</label>
          <div>
            <InputText
              pt:root:class="w-full"
              v-model="formCommand"
              id="command"
              :class="{ 'p-invalid': errors.command }"
            />
          </div>
        </div>
        <div class="field">
          <label for="response">Response</label>
          <div>
            <Textarea
              rows="10"
              cols="40"
              pt:root:class="w-full"
              pt:input:root:class="w-full"
              v-model="response"
              id="response"
              :class="{ 'p-invalid': errors.response }"
            ></Textarea>
          </div>
        </div>
        <div class="flex justify-content-end">
          <div class="flex-1 mr-2">
            <Button
              :disabled="!meta.valid"
              label="Save"
              icon="pi pi-save"
              severity="success"
              @click="onSubmit"
              class="w-full"
            >
            </Button>
          </div>
          <div class="flex-1">
            <Button
              label="Cancel"
              icon="pi pi-times"
              severity="warning"
              @click="onCancel"
              class="w-full"
            >
            </Button>
          </div>
        </div>
      </form>
    </div>
  </Suspense>
</template>

<style scoped></style>

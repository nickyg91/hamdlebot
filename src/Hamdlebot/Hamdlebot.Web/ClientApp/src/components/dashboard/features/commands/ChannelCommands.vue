<script setup lang="ts">
import type { IBotChannelCommand } from '@/components/dashboard/models/bot-commands.interface';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import Button from 'primevue/button';
import { useDialog } from 'primevue/usedialog';
import ChannelCommandForm from './ChannelCommandForm.vue';
import { h } from 'vue';
import { useDashboardStore } from '@/components/dashboard/stores/dashboard.store';
import { useBotChannelCommands } from '@/composables/bot-channel-commands.composable';
import ConfirmDialog from 'primevue/confirmdialog';
import { useConfirm } from 'primevue/useconfirm';

const props = defineProps<{
  channelId: number;
  commands: IBotChannelCommand[];
}>();
const dashboardStore = useDashboardStore();
const { removeChannelCommand } = useBotChannelCommands(props.channelId);
const dialog = useDialog();
const confirm = useConfirm();

const onEditClicked = (command: IBotChannelCommand) => {
  const component = h(ChannelCommandForm, {
    command: command,
    channelId: props.channelId,
    'onSubmit:command': (command: IBotChannelCommand) => {
      dashboardStore.updateCommand(command);
      instance.close();
    },
    'onCancel:command': () => {
      instance.close();
    }
  });
  const instance = dialog.open(component, {
    props: {
      header: 'Channel Command',
      style: {
        width: '50vw'
      },
      breakpoints: {
        '960px': '75vw',
        '640px': '90vw'
      },
      modal: true,
      draggable: false
    },
    data: {}
  });
};

const onAddClicked = () => {
  const component = h(ChannelCommandForm, {
    command: null,
    channelId: props.channelId,
    'onSubmit:command': async (command: IBotChannelCommand) => {
      dashboardStore.addCommandToChannel(command);
      instance.close();
    },
    'onCancel:command': () => {
      instance.close();
    }
  });

  const instance = dialog.open(component, {
    props: {
      header: 'Channel Command',
      style: {
        width: '50vw'
      },
      breakpoints: {
        '960px': '75vw',
        '640px': '90vw'
      },
      modal: true,
      draggable: false
    },
    data: {}
  });
};
const onDeleteClicked = async (command: IBotChannelCommand) => {
  confirm.require({
    message: 'Are you sure you want to delete this? This action cannot be undone.',
    header: 'Are you sure?',
    icon: 'pi pi-exclamation-triangle',
    rejectClass: 'p-button-secondary p-button-outlined',
    rejectLabel: 'Cancel',
    acceptLabel: 'Ok',
    accept: async () => {
      try {
        await removeChannelCommand(command.id);
        dashboardStore.removeCommand(command);
      } catch (error) {
        console.error(error);
      }
    }
  });
};
</script>
<template>
  <div>
    <div class="flex align-items-center">
      <div><h3>Channel Commands</h3></div>
      <div>
        <Button
          @click="onAddClicked"
          size="small"
          rounded
          outlined
          icon="pi pi-plus"
          severity="success"
          class="ml-4"
        >
        </Button>
      </div>
    </div>
    <DataTable :value="commands">
      <Column field="command" header="Command"></Column>
      <Column field="response" header="Response"></Column>
      <Column :body-style="'width:1px'">
        <template #body="{ data }">
          <Button @click="onEditClicked(data)" rounded icon="pi pi-pencil" severity="info">
          </Button>
        </template>
      </Column>
      <Column :body-style="'width:1px'">
        <template #body="{ data }">
          <Button @click="onDeleteClicked(data)" rounded icon="pi pi-times" severity="danger">
          </Button>
        </template>
      </Column>
    </DataTable>
  </div>
</template>

<style scoped></style>

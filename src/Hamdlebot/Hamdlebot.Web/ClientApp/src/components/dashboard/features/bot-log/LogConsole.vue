<script setup lang="ts">
import { defineProps } from 'vue';
import type { ILogMessage } from '@/components/dashboard/models/log-message.interface';
defineProps<{ messages: ILogMessage[] }>();
</script>
<template>
  <div></div>
</template>
<style scoped></style>

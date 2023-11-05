<script setup lang="ts">
const props = defineProps<{ currentWord: string }>();
</script>
<template>
  <div>
    {{ props.currentWord }}
  </div>
</template>

<style scoped></style>

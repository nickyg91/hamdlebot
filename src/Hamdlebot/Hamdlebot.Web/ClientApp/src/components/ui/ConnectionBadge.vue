<script setup lang="ts">
const props = defineProps<{ size: 'small' | 'medium' | 'large'; color: string; label?: string }>();
</script>
<template>
  <span class="badge-container flex align-items-center">
    <label class="text-sm mr-2" v-if="label">
      {{ label }}
    </label>
    <div
      class="connection-badge"
      :class="{ small: size === 'small', medium: size === 'medium', large: size === 'large' }"
      :style="`background-color:${color}`"
    ></div>
  </span>
</template>

<style scoped>
.badge-container {
  padding: 0.01em;
}

.connection-badge {
  border-radius: 10px;
  transform: scale(1);
  animation: pulse 2s infinite;
  box-shadow: 0px 0px 1px 1px rgb(v-bind('props.color'));
}

@keyframes pulse {
  0% {
    transform: scale(0.95);
    box-shadow: 0 0 0 0 rgb(from v-bind('props.color') r g b / 0.7);
  }

  70% {
    transform: scale(1.15);
    box-shadow: 0 0 0 15px rgb(from v-bind('props.color') r g b / 0.00001);
  }

  100% {
    transform: scale(0.95);
    box-shadow: 0 0 0 0 rgb(from v-bind('props.color') r g b / 0.0000001);
  }
}
.small {
  width: 0.5em;
  height: 0.5em;
}

.medium {
  width: 0.75em;
  height: 0.75em;
}

.large {
  width: 1em;
  height: 1em;
}
</style>

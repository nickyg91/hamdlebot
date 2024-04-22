<script setup lang="ts">
defineProps<{
  letter: string;
  isGuessed: boolean;
  isCorrect: boolean;
  isInCorrectPosition: boolean;
}>();
</script>
<template>
  <div
    :class="{
      'incorrect-letter': !isCorrect && isGuessed,
      'bg-yellow-300': isCorrect && isGuessed && !isInCorrectPosition,
      correct: isGuessed && isCorrect && isInCorrectPosition
    }"
    class="border-5 letter flex justify-content-center p-3 mr-2"
  >
    {{ letter }}
  </div>
</template>

<style scoped>
.letter {
  font-size: 1.75em;
}
.border-5 {
  border-radius: 5px;
  border: 2px white solid;
}
.incorrect-letter {
  background-color: darkgray;
}
.correct {
  background-color: #0072b2;
}
</style>

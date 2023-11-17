<script setup lang="ts">
import { computed } from 'vue';
import { type IPositionalLetter } from '../models/positional-letter.interface';

const props = defineProps<{ currentWord: string; guess: string | null | undefined }>();
const letters = computed(() => {
  if (!props.guess) {
    return [null, null, null, null, null];
  }
  const letters: IPositionalLetter[] = [];
  for (let i = 0; i < 5; i++) {
    letters.push({
      isCorrect: props.guess.charAt(i).toLowerCase() === props.currentWord.charAt(i).toLowerCase(),
      letter: props.guess.charAt(i).toUpperCase()
    });
  }
  return letters;
});
</script>
<template>
  <div>
    <template v-for="(letter, index) in letters" :key="index">
      <div
        :class="{
          'bg-yellow-300': letter && !letter.isCorrect,
          correct: letter && letter.isCorrect
        }"
        class="letter flex p-3 mr-2"
      >
        {{ letter?.letter }}
      </div>
    </template>
  </div>
</template>

<style scoped>
.letter {
  border-radius: 5px;
  border: 2px white solid;
  font-size: 1.75em;
  height: 72px;
  width: 56px;
  color: white;
  text-shadow: black;
}

.correct {
  background-color: #0072b2;
}
</style>

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
      letter: props.guess.charAt(i).toUpperCase(),
      isInWord: props.currentWord.indexOf(props.guess.charAt(i)) > -1
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
          'bg-yellow-300': letter && !letter.isCorrect && letter.isInWord,
          correct: letter && letter.isCorrect,
          'wrong-letter': letter && !letter.isCorrect && !letter.isInWord
        }"
        class="letter flex justify-content-center p-3 mr-2"
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

.wrong-letter {
  background-color: darkgray;
}
</style>

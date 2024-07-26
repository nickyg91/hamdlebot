<script setup lang="ts">
import { useHamdleStore } from '@/components/hamdle/stores/hamdle.store';
import GuessedLetter from './GuessedLetter.vue';
const store = useHamdleStore();

const letterRows = [
  ['q', 'w', 'e', 'r', 't', 'y', 'u', 'i', 'o', 'p'],
  ['a', 's', 'd', 'f', 'g', 'h', 'j', 'k', 'l'],
  ['z', 'x', 'c', 'v', 'b', 'n', 'm']
];

const isGuessed = (letter: string) => {
  const uniqueLetters = new Set<string>();
  store.guesses.map((guess) => {
    let guessedLetters = Array.from(guess);
    guessedLetters.map((letter) => {
      uniqueLetters.add(letter);
    });
  });
  return uniqueLetters.has(letter);
};

const isInGuess = (letter: string) => {
  return store.currentWord.includes(letter);
};
</script>
<template>
  <div class="flex p-2" v-for="(row, index) in letterRows" :key="index">
    <div v-for="letter in row" :key="letter">
      <GuessedLetter
        :is-guessed="isGuessed(letter)"
        :is-correct="isInGuess(letter)"
        :letter="letter"
      ></GuessedLetter>
    </div>
  </div>
</template>

<style scoped></style>

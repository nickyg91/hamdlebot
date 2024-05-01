import './assets/main.css';

import { createApp } from 'vue';
import { createPinia } from 'pinia';

import App from './App.vue';
import router from './router';
import PrimeVue from 'primevue/config';
import 'primeicons/primeicons.css';
import { useSignalR } from './composables/signalr.composable';
const app = createApp(App);

app.use(PrimeVue, { ripple: true });
app.use(createPinia());
app.use(router);

app.mount('#app');

import './assets/main.css';

import { createApp } from 'vue';
import { createPinia } from 'pinia';

import App from './App.vue';
import router from './router';
import PrimeVue from 'primevue/config';
import { useSignalR } from './composables/signalr.composable';
const app = createApp(App);

const { createSignalRConnection } = useSignalR();

await createSignalRConnection('botloghub');
await createSignalRConnection('hamdlebothub');

app.use(PrimeVue);
app.use(createPinia());
app.use(router);

app.mount('#app');

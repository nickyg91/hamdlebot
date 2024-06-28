import './assets/main.css';
import { createApp } from 'vue';
import { createPinia } from 'pinia';
import App from './App.vue';
import router from './router';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import 'primeicons/primeicons.css';
import ConfirmationService from 'primevue/confirmationservice';
import DialogService from 'primevue/dialogservice';

const app = createApp(App);
app.use(PrimeVue, { ripple: true });
app.use(ToastService);
app.use(router);
app.use(createPinia());
app.use(ConfirmationService);
app.use(DialogService);
app.mount('#app');

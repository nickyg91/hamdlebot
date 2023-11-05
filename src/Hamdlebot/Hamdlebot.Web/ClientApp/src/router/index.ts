import HamdleView from '@/views/HamdleView.vue';
import { createRouter, createWebHistory } from 'vue-router';

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'hamdle',
      component: HamdleView
    }
  ]
});

export default router;

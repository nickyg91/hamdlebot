import HamdleView from '@/views/HamdleView.vue';
import AuthenticateView from '@/views/AuthenticateView.vue';
import { createRouter, createWebHistory } from 'vue-router';
import DashboardView from '@/views/DashboardView.vue';

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'hamdle',
      component: HamdleView
    },
    {
      path: '/authenticate',
      name: 'authenticate',
      component: AuthenticateView
    },
    {
      path: '/authenticated',
      name: 'authenticated',
      component: AuthenticateView
    },
    {
      path: '/dashboard',
      name: 'dashboard',
      component: DashboardView
    }
  ]
});

export default router;

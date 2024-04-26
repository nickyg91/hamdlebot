import HamdleView from '@/views/HamdleView.vue';
import { createRouter, createWebHistory } from 'vue-router';
import DashboardView from '@/views/DashboardView.vue';
import AuthenticateView from '@/views/AuthenticateView.vue';

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/hamdle',
      name: 'hamdle',
      component: HamdleView
    },
    {
      path: '/',
      redirect: '/dashboard'
    },
    {
      path: '/dashboard',
      name: 'dashboard',
      component: DashboardView
    },
    {
      path: '/authenticate',
      name: 'authenticate',
      component: AuthenticateView
    }
  ]
});

export default router;

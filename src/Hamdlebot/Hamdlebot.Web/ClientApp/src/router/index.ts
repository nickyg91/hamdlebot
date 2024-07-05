import HamdleView from '@/views/HamdleView.vue';
import { createRouter, createWebHistory } from 'vue-router';
import DashboardView from '@/views/DashboardView.vue';
import AuthenticateView from '@/views/AuthenticateView.vue';
import BotAuthenticateView from '@/views/BotAuthenticateView.vue';

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/:twitchUserId/hamdle',
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
    },
    {
      path: '/bot/authenticate',
      name: 'bot-authenticate',
      component: BotAuthenticateView
    },
    {
      path: '/:pathMatch(.*)*',
      redirect: '/dashboard'
    }
  ]
});

export default router;

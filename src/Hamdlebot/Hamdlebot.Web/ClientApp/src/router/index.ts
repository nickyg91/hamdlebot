import { createRouter, createWebHistory } from 'vue-router';
const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/:twitchUserId/hamdle',
      name: 'hamdle',
      component: () => import('@/views/HamdleView.vue')
    },
    {
      path: '/',
      redirect: '/dashboard'
    },
    {
      path: '/dashboard',
      name: 'dashboard',
      component: () => import('@/views/DashboardView.vue')
    },
    {
      path: '/authenticate',
      name: 'authenticate',
      component: () => import('@/views/AuthenticateView.vue')
    },
    {
      path: '/bot/authenticate',
      name: 'bot-authenticate',
      component: () => import('@/views/BotAuthenticateView.vue')
    },
    {
      path: '/:pathMatch(.*)*',
      redirect: '/dashboard'
    }
  ]
});

export default router;

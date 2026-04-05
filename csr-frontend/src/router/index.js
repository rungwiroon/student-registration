import { createRouter, createWebHistory } from 'vue-router'

const routes = [
  {
    path: '/',
    name: 'Register',
    component: () => import('../views/Register.vue')
  }
]

export const router = createRouter({
  history: createWebHistory(),
  routes
})

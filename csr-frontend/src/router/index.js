import { createRouter, createWebHistory } from 'vue-router'
import MainLayout from '../components/MainLayout.vue'

const routes = [
  {
    path: '/register',
    name: 'Register',
    component: () => import('../views/Register.vue')
  },
  {
    path: '/profile/edit',
    name: 'EditProfile',
    component: () => import('../views/Register.vue')
  },
  {
    path: '/',
    component: MainLayout,
    redirect: '/dashboard',
    children: [
      {
        path: 'dashboard',
        name: 'Dashboard',
        component: () => import('../views/Dashboard.vue')
      },
      {
        path: 'class-list',
        name: 'ClassList',
        component: () => import('../views/ClassList.vue')
      },
      {
        path: 'contacts',
        name: 'Contacts',
        component: () => import('../views/Contacts.vue')
      }
    ]
  }
]

export const router = createRouter({
  history: createWebHistory(),
  routes
})

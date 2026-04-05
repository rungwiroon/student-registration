import { createRouter, createWebHistory } from 'vue-router'
import MainLayout from '../components/MainLayout.vue'
import BackofficeLayout from '../components/BackofficeLayout.vue'

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
    path: '/document',
    name: 'IntroductionDocument',
    component: () => import('../views/IntroductionDocumentView.vue')
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
  },
  {
    path: '/backoffice',
    component: BackofficeLayout,
    redirect: '/backoffice/dashboard',
    children: [
      {
        path: 'dashboard',
        name: 'BackofficeDashboard',
        component: () => import('../views/backoffice/DashboardView.vue')
      },
      {
        path: 'students',
        name: 'BackofficeStudentList',
        component: () => import('../views/backoffice/StudentListView.vue')
      },
      {
        path: 'students/:id',
        name: 'BackofficeStudentDetail',
        component: () => import('../views/backoffice/StudentDetailView.vue')
      }
    ]
  }
]

export const router = createRouter({
  history: createWebHistory(),
  routes
})

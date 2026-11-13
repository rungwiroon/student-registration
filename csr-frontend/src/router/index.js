import { createRouter, createWebHistory } from 'vue-router'
import MainLayout from '../components/MainLayout.vue'
import BackofficeLayout from '../components/BackofficeLayout.vue'
import { redirectTargetFromQuery, rememberRedirectTarget } from '../utils/postLoginRedirect'

const routes = [
  {
    path: '/register',
    name: 'Register',
    component: () => import('../views/Register.vue'),
    meta: {
      frontofficeHeader: {
        title: 'ลงทะเบียนข้อมูล',
        showBack: true,
        showHome: true
      }
    }
  },
  {
    path: '/profile/edit',
    name: 'EditProfile',
    component: () => import('../views/Register.vue'),
    meta: {
      frontofficeHeader: {
        title: 'แก้ไขข้อมูล',
        showBack: true,
        showHome: true
      }
    }
  },
  {
    path: '/document',
    name: 'IntroductionDocument',
    component: () => import('../views/IntroductionDocumentView.vue'),
    meta: {
      frontofficeHeader: {
        title: 'เอกสารแนะนำนักเรียนและครอบครัว',
        showBack: true,
        showHome: true
      }
    }
  },
  {
    path: '/shirt-order',
    name: 'ShirtOrder',
    component: () => import('../views/ShirtOrder.vue'),
    meta: {
      frontofficeHeader: {
        title: 'สั่งซื้อเสื้อ POLO ม.1/2',
        showBack: true,
        showHome: true
      }
    }
  },
  {
    path: '/venue-vote',
    name: 'VenueVote',
    component: () => import('../views/VenueVote.vue'),
    meta: {
      frontofficeHeader: {
        title: 'โหวตสถานที่จัดกิจกรรม',
        showBack: true,
        showHome: true
      }
    }
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
      },
      {
        path: 'staff',
        name: 'BackofficeStaff',
        component: () => import('../views/backoffice/StaffManagementView.vue')
      },
      {
        path: 'registered-users',
        name: 'BackofficeRegisteredUsers',
        component: () => import('../views/backoffice/RegisteredUsersView.vue')
      },
      {
        path: 'venue-vote',
        name: 'BackofficeVenueVote',
        component: () => import('../views/backoffice/VenueVoteSummaryView.vue')
      }
    ]
  }
]

export const router = createRouter({
  history: createWebHistory(),
  routes
})

// Deep links: a LIFF link such as https://liff.line.me/<id>?redirect=/venue-vote
// arrives with the target in `redirect` or `liff.state`. If we already hold a
// token, go straight there; otherwise remember it so /liff-entry.html can
// return to it after login.
router.beforeEach((to) => {
  const target = redirectTargetFromQuery(to.query)
  if (!target || target === to.fullPath) return

  const hasToken = import.meta.env.VITE_USE_MOCK_LIFF === 'true' || !!window.localStorage.getItem('liff_access_token')
  const isLoginCallback = !!(to.query.code && to.query.state)
  if (hasToken && !isLoginCallback) return target

  rememberRedirectTarget(target)
})

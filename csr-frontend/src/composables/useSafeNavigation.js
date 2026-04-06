import { useRouter } from 'vue-router'

const HOME_ROUTE = '/dashboard'

export function useSafeNavigation() {
  const router = useRouter()

  function goBackOrFallback(fallbackRoute = HOME_ROUTE) {
    if (window.history.length > 1) {
      router.back()
    } else {
      router.push(fallbackRoute)
    }
  }

  function goHome() {
    router.push(HOME_ROUTE)
  }

  return { goBackOrFallback, goHome }
}

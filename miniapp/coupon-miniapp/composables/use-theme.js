import { computed } from 'vue'
import { useSessionStore } from '@/store/session'

export function useTheme() {
  const session = useSessionStore()

  const themeCode = computed(() => (session.themeCode === 'light' ? 'light' : 'green'))
  const themeClass = computed(() => `theme-${themeCode.value}`)

  return {
    themeCode,
    themeClass
  }
}

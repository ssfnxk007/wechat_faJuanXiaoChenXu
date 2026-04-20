import { computed } from 'vue'
import { useSessionStore } from '@/store/session'

const VALID_CODES = ['green', 'light', 'candy', 'orange', 'red']

export function useTheme() {
  const session = useSessionStore()

  const themeCode = computed(() => {
    return VALID_CODES.includes(session.themeCode) ? session.themeCode : 'green'
  })
  const themeClass = computed(() => `theme-${themeCode.value}`)

  return {
    themeCode,
    themeClass
  }
}

import { computed } from 'vue'
import { useSessionStore } from '@/store/session'
import { VALID_THEME_CODES, DEFAULT_THEME_CODE } from '@/constants/theme'

export function useTheme() {
  const session = useSessionStore()

  const themeCode = computed(() => {
    return VALID_THEME_CODES.includes(session.themeCode) ? session.themeCode : DEFAULT_THEME_CODE
  })
  const themeClass = computed(() => `theme-${themeCode.value}`)

  return {
    themeCode,
    themeClass
  }
}

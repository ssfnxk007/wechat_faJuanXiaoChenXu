const ACCESS_TOKEN_KEY = 'fajuan_admin_access_token'
const ADMIN_USERNAME_KEY = 'fajuan_admin_username'
const ADMIN_DISPLAY_NAME_KEY = 'fajuan_admin_display_name'
const ADMIN_MENU_PATHS_KEY = 'fajuan_admin_menu_paths'
const ADMIN_PERMISSION_CODES_KEY = 'fajuan_admin_permission_codes'

const readJsonArray = (value: string | null) => {
  if (!value) return [] as string[]
  try {
    const parsed = JSON.parse(value)
    return Array.isArray(parsed) ? parsed : []
  } catch {
    return [] as string[]
  }
}

export const authStorage = {
  getAccessToken: () => localStorage.getItem(ACCESS_TOKEN_KEY) || '',
  setAccessToken: (token: string) => localStorage.setItem(ACCESS_TOKEN_KEY, token),
  clearAccessToken: () => localStorage.removeItem(ACCESS_TOKEN_KEY),
  getUsername: () => localStorage.getItem(ADMIN_USERNAME_KEY) || '',
  setUsername: (username: string) => localStorage.setItem(ADMIN_USERNAME_KEY, username),
  clearUsername: () => localStorage.removeItem(ADMIN_USERNAME_KEY),
  getDisplayName: () => localStorage.getItem(ADMIN_DISPLAY_NAME_KEY) || '',
  setDisplayName: (displayName: string) => localStorage.setItem(ADMIN_DISPLAY_NAME_KEY, displayName),
  clearDisplayName: () => localStorage.removeItem(ADMIN_DISPLAY_NAME_KEY),
  getMenuPaths: () => readJsonArray(localStorage.getItem(ADMIN_MENU_PATHS_KEY)),
  setMenuPaths: (paths: string[]) => localStorage.setItem(ADMIN_MENU_PATHS_KEY, JSON.stringify(paths || [])),
  clearMenuPaths: () => localStorage.removeItem(ADMIN_MENU_PATHS_KEY),
  getPermissionCodes: () => readJsonArray(localStorage.getItem(ADMIN_PERMISSION_CODES_KEY)),
  setPermissionCodes: (codes: string[]) => localStorage.setItem(ADMIN_PERMISSION_CODES_KEY, JSON.stringify(codes || [])),
  clearPermissionCodes: () => localStorage.removeItem(ADMIN_PERMISSION_CODES_KEY),
  hasPermission: (code: string) => readJsonArray(localStorage.getItem(ADMIN_PERMISSION_CODES_KEY)).includes(code),
  clearAll: () => {
    localStorage.removeItem(ACCESS_TOKEN_KEY)
    localStorage.removeItem(ADMIN_USERNAME_KEY)
    localStorage.removeItem(ADMIN_DISPLAY_NAME_KEY)
    localStorage.removeItem(ADMIN_MENU_PATHS_KEY)
    localStorage.removeItem(ADMIN_PERMISSION_CODES_KEY)
  },
}

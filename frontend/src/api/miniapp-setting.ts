import http from './http'
import type { ApiResponse } from '@/types/api'
import type { MiniAppThemeSettingsDto, SaveMiniAppThemeSettingsRequest } from '@/types/miniapp-setting'

export const getMiniAppThemeSettings = () =>
  http.get<never, ApiResponse<MiniAppThemeSettingsDto>>('/AdminMiniAppTheme')

export const updateMiniAppThemeSettings = (payload: SaveMiniAppThemeSettingsRequest) =>
  http.put<SaveMiniAppThemeSettingsRequest, ApiResponse<MiniAppThemeSettingsDto>>('/AdminMiniAppTheme', payload)

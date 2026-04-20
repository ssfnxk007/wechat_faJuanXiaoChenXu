import http from './http'
import type { ApiResponse } from '@/types/api'
import type {
  AdminWeChatPaySettingsDto,
  SaveAdminWeChatPaySettingsRequest,
} from '@/types/miniapp-pay-setting'

export const getMiniAppPaySettings = () =>
  http.get<never, ApiResponse<AdminWeChatPaySettingsDto>>('/AdminMiniAppPay')

export const updateMiniAppPaySettings = (payload: SaveAdminWeChatPaySettingsRequest) =>
  http.put<SaveAdminWeChatPaySettingsRequest, ApiResponse<AdminWeChatPaySettingsDto>>(
    '/AdminMiniAppPay',
    payload,
  )

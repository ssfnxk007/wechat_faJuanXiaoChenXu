export interface AdminWeChatPaySettingsDto {
  appId: string
  merchantId: string
  merchantSerialNo: string
  privateKeyDisplay: string
  apiV3KeyDisplay: string
  notifyUrl: string
  enableMockFallback: boolean
  isConfigured: boolean
  updatedAt: string | null
}

export interface SaveAdminWeChatPaySettingsRequest {
  appId?: string
  merchantId?: string
  merchantSerialNo?: string
  privateKeyPem?: string
  apiV3Key?: string
  notifyUrl?: string
  enableMockFallback?: boolean
}

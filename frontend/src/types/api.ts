export interface ApiResponse<T> {
  code: number
  message: string
  data: T
  success: boolean
}

export interface HealthStatusDto {
  service: string
  version: string
  environment: string
  serverTime: string
}

export interface AdminLoginRequest {
  username: string
  password: string
}

export interface AdminLoginResultDto {
  accessToken: string
  username: string
  expiresAt: string
}

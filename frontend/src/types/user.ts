export interface UserListItemDto {
  id: number
  miniOpenId: string
  mobile?: string
  nickname?: string
  createdAt: string
}

export interface MiniProgramLoginRequest {
  code: string
  nickname?: string
}

export interface BindMobileRequest {
  userId: number
  mobile: string
}

export interface AuthLoginResultDto {
  userId: number
  miniOpenId: string
  mobile?: string
  isNewUser: boolean
}

export interface AdminAuthProfileDto {
  userId: number
  username: string
  displayName: string
  isFallbackAdmin: boolean
  roleCodes: string[]
  roleNames: string[]
  menuPaths: string[]
  permissionCodes: string[]
}

export interface AdminUserListItemDto {
  id: number
  username: string
  displayName: string
  isEnabled: boolean
  createdAt: string
  roleIds: number[]
  roleNames: string
}

export interface SaveAdminUserRequest {
  username: string
  password?: string
  displayName: string
  isEnabled: boolean
  roleIds: number[]
}

export interface ResetAdminUserPasswordRequest {
  password: string
}

export interface AdminRoleListItemDto {
  id: number
  name: string
  code: string
  isEnabled: boolean
  createdAt: string
  userCount: number
  menuCount: number
  permissionCount: number
  menuIds: number[]
  permissionIds: number[]
}

export interface SaveAdminRoleRequest {
  name: string
  code: string
  isEnabled: boolean
  menuIds: number[]
  permissionIds: number[]
}

export interface AdminMenuListItemDto {
  id: number
  parentId?: number
  name: string
  path: string
  component: string
  sort: number
  isEnabled: boolean
  createdAt: string
  children: AdminMenuListItemDto[]
}

export interface SaveAdminMenuRequest {
  parentId?: number
  name: string
  path: string
  component: string
  sort: number
  isEnabled: boolean
}

export interface AdminPermissionListItemDto {
  id: number
  name: string
  code: string
  menuPath: string
  sort: number
  isEnabled: boolean
  createdAt: string
}

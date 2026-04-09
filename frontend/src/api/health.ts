import http from './http'
import type { ApiResponse, HealthStatusDto } from '@/types/api'

export const getHealth = () => http.get<never, ApiResponse<HealthStatusDto>>('/health')

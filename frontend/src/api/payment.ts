import http from './http'
import type { ApiResponse } from '@/types/api'
import type { CreatePaymentRequest, CreatePaymentResultDto, PaymentCallbackRequest } from '@/types/payment'

export const createPayment = (payload: CreatePaymentRequest) =>
  http.post<CreatePaymentRequest, ApiResponse<CreatePaymentResultDto>>('/payments/create', payload)

export const paymentCallback = (payload: PaymentCallbackRequest) =>
  http.post<PaymentCallbackRequest, ApiResponse<boolean>>('/payments/callback', payload)

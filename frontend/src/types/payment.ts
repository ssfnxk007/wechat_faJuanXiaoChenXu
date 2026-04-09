export interface CreatePaymentRequest {
  orderId: number
}

export interface CreatePaymentResultDto {
  paymentTransactionId: number
  paymentNo: string
  amount: number
  mockPayToken: string
}

export interface PaymentCallbackRequest {
  paymentNo: string
  channelTradeNo?: string
  success: boolean
  rawCallback?: string
}

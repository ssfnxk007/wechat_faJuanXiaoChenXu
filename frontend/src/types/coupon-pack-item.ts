export interface CouponPackItemDto {
  id: number
  couponPackId: number
  couponTemplateId: number
  quantity: number
  couponTemplateName: string
}

export interface SaveCouponPackItemRequest {
  couponPackId: number
  couponTemplateId: number
  quantity: number
}

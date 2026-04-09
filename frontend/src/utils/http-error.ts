export const getErrorMessage = (error: unknown, fallback = '操作失败，请稍后重试') => {
  const responseMessage = (error as { response?: { data?: { message?: string } } })?.response?.data?.message
  if (responseMessage && typeof responseMessage === 'string') {
    return responseMessage
  }

  const directMessage = (error as { message?: string })?.message
  if (directMessage && typeof directMessage === 'string') {
    return directMessage
  }

  return fallback
}

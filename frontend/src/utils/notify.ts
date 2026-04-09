import { reactive } from 'vue'

export interface ToastItem {
  id: number
  type: 'success' | 'error' | 'info'
  message: string
}

const state = reactive({
  items: [] as ToastItem[],
})

let seed = 1

const push = (type: ToastItem['type'], message: string, duration = 2600) => {
  const item: ToastItem = { id: seed++, type, message }
  state.items.push(item)

  window.setTimeout(() => {
    const index = state.items.findIndex((current) => current.id === item.id)
    if (index >= 0) {
      state.items.splice(index, 1)
    }
  }, duration)
}

export const notify = {
  state,
  success(message: string) {
    push('success', message)
  },
  error(message: string) {
    push('error', message, 3200)
  },
  info(message: string) {
    push('info', message)
  },
  remove(id: number) {
    const index = state.items.findIndex((item) => item.id === id)
    if (index >= 0) {
      state.items.splice(index, 1)
    }
  },
}

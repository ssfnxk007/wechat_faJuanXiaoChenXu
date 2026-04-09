<template>
  <div class="toast-stack">
    <div
      v-for="item in notify.state.items"
      :key="item.id"
      :class="['toast-item', item.type]"
      @click="notify.remove(item.id)"
    >
      <strong>{{ titleMap[item.type] }}</strong>
      <span>{{ item.message }}</span>
    </div>
  </div>
</template>

<script setup lang="ts">
import { notify } from '@/utils/notify'

const titleMap = {
  success: '成功',
  error: '失败',
  info: '提示',
}
</script>

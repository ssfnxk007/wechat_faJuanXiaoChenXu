<template>
  <div class="toast-stack">
    <div
      v-for="item in notify.state.items"
      :key="item.id"
      :class="['toast-item', item.type]"
      @click="notify.remove(item.id)"
    >
      <div class="toast-item-head">
        <span :class="['toast-mark', item.type]"></span>
        <strong>{{ titleMap[item.type] }}</strong>
      </div>
      <span>{{ item.message }}</span>
    </div>
  </div>
</template>

<script setup lang="ts">
import { notify } from '@/utils/notify'

const titleMap = {
  success: '操作成功',
  error: '处理失败',
  info: '提示信息',
}
</script>

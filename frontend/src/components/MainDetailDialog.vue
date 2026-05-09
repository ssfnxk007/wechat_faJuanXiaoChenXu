<template>
  <div class="dialog-mask" @click.self="emit('close')">
    <div :class="['dialog-card-compact', sizeClass]" role="dialog" aria-modal="true">
      <header class="dialog-card-head">
        <div>
          <h3>{{ title }}</h3>
          <p v-if="sub">{{ sub }}</p>
        </div>
        <button type="button" class="dialog-close" aria-label="关闭" @click="emit('close')">×</button>
      </header>
      <div class="dialog-body">
        <slot />
      </div>
      <footer v-if="$slots.footer" class="dialog-footer">
        <slot name="footer" />
      </footer>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'

type DialogSize = 'sm' | 'md' | 'lg' | 'xl'

const props = withDefaults(
  defineProps<{
    title: string
    sub?: string
    size?: DialogSize
  }>(),
  { sub: '', size: 'md' },
)

const emit = defineEmits<{ close: [] }>()

const sizeClass = computed(() => `size-${props.size}`)
</script>

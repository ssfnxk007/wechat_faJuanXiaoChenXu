<template>
  <div class="remote-select-field">
    <div class="remote-select-search">
      <input :value="keyword" type="text" :placeholder="placeholder" @input="handleInput" @keyup.enter="emitSearch" />
      <button type="button" class="ghost-button" @click="emitSearch">搜索</button>
    </div>
    <select :value="modelValue" @change="handleChange">
      <option :value="emptyValue">{{ emptyLabel }}</option>
      <option v-for="option in options" :key="option.value" :value="option.value">{{ option.label }}</option>
    </select>
  </div>
</template>

<script setup lang="ts">
export interface RemoteSelectOption {
  value: number
  label: string
}

const props = defineProps<{
  modelValue: number
  keyword: string
  placeholder: string
  emptyLabel: string
  emptyValue?: number
  options: RemoteSelectOption[]
}>()

const emit = defineEmits<{
  'update:modelValue': [value: number]
  'update:keyword': [value: string]
  search: []
}>()

const handleInput = (event: Event) => {
  emit('update:keyword', (event.target as HTMLInputElement).value)
}

const handleChange = (event: Event) => {
  emit('update:modelValue', Number((event.target as HTMLSelectElement).value))
}

const emitSearch = () => emit('search')
</script>

<style scoped>
.remote-select-field {
  display: grid;
  gap: 8px;
}

.remote-select-search {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto;
  gap: 10px;
}

.remote-select-field input,
.remote-select-field select {
  width: 100%;
  height: 44px;
  padding: 0 14px;
  border: 1px solid var(--line-strong);
  border-radius: 12px;
  background: #fff;
}
</style>

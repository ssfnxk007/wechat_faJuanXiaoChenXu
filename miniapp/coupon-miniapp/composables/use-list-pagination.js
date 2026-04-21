import { ref } from 'vue'

/**
 * 列表分页状态机。
 * @param {Function} fetcher - async ({ pageIndex, pageSize, ...extraQuery }) => { items, pageIndex, totalPages, totalCount }
 * @param {Object}   options
 * @param {number}   options.pageSize - 每页条数，默认 10
 * @param {Function} options.buildQuery - 可选，返回额外查询参数（如状态筛选），每次请求前调用
 */
export function useListPagination(fetcher, options = {}) {
  const pageSize = options.pageSize ?? 10
  const buildQuery = options.buildQuery ?? (() => ({}))

  const items = ref([])
  const refreshing = ref(false)
  const loadingMore = ref(false)
  const noMore = ref(false)
  const pageIndex = ref(1)
  const totalCount = ref(0)

  async function load(targetPage, mode) {
    const query = { ...buildQuery(), pageIndex: targetPage, pageSize }
    const result = await fetcher(query)
    const resultItems = Array.isArray(result?.items) ? result.items : []
    const totalPages = Number(result?.totalPages) || 1

    if (mode === 'refresh') {
      items.value = resultItems
    } else {
      items.value = items.value.concat(resultItems)
    }
    pageIndex.value = Number(result?.pageIndex) || targetPage
    totalCount.value = Number(result?.totalCount) || items.value.length
    noMore.value = pageIndex.value >= totalPages || resultItems.length === 0
  }

  async function onRefresh() {
    if (refreshing.value) return
    refreshing.value = true
    try {
      await load(1, 'refresh')
    } catch (error) {
      console.warn('[pagination] refresh failed', error)
      uni.showToast({ title: error?.message || '加载失败', icon: 'none' })
    } finally {
      refreshing.value = false
    }
  }

  async function onLoadMore() {
    if (loadingMore.value || noMore.value || refreshing.value) return
    loadingMore.value = true
    const nextPage = pageIndex.value + 1
    try {
      await load(nextPage, 'append')
    } catch (error) {
      console.warn('[pagination] load more failed', error)
      uni.showToast({ title: error?.message || '加载失败', icon: 'none' })
    } finally {
      loadingMore.value = false
    }
  }

  return {
    items,
    refreshing,
    loadingMore,
    noMore,
    pageIndex,
    pageSize,
    totalCount,
    onRefresh,
    onLoadMore,
    reload: onRefresh
  }
}

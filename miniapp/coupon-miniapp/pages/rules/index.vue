<template>
  <view :class="['cm-page', themeClass]">
    <view class="cm-container">
      <view class="cm-nav-spacer"></view>

      <view class="page-topbar">
        <view class="page-back" @click="goBack">‹ 返回</view>
      </view>

      <view class="summary-card cm-card">
        <view>
          <text class="summary-title">使用规则</text>
          <text class="summary-subtitle">领券、购券、核销前查看</text>
        </view>
        <view class="summary-badge">规则</view>
      </view>

      <view class="brief-card cm-card cm-section">
        <view class="brief-item" v-for="item in briefItems" :key="item.label">
          <text class="brief-label">{{ item.label }}</text>
          <text class="brief-value">{{ item.value }}</text>
        </view>
      </view>

      <view class="cm-section">
        <SectionHeader eyebrow="APPLICABLE" title="适用说明" subtitle="先看范围" />
        <view class="content-block cm-card">
          <view class="rule-row" v-for="item in applicableRules" :key="item.title">
            <view class="rule-index">{{ item.index }}</view>
            <view class="rule-body">
              <text class="rule-title">{{ item.title }}</text>
              <text class="rule-text">{{ item.text }}</text>
            </view>
          </view>
        </view>
      </view>

      <view class="cm-section">
        <SectionHeader eyebrow="USAGE RULES" title="使用规则" subtitle="领取、下单、核销" />
        <view class="content-block cm-card">
          <view class="list-item" v-for="item in usageRules" :key="item.title">
            <text class="list-title">{{ item.title }}</text>
            <text class="list-text">{{ item.text }}</text>
          </view>
        </view>
      </view>

      <view class="cm-section two-column-section">
        <view class="faq-card cm-card">
          <text class="panel-title">失效与限制</text>
          <view class="panel-list">
            <view class="panel-item" v-for="item in invalidRules" :key="item">
              <text class="panel-dot"></text>
              <text class="panel-text">{{ item }}</text>
            </view>
          </view>
        </view>

        <view class="faq-card cm-card">
          <text class="panel-title">常见问题</text>
          <view class="qa-list">
            <view class="qa-item" v-for="item in qaItems" :key="item.q">
              <text class="qa-question">{{ item.q }}</text>
              <text class="qa-answer">{{ item.a }}</text>
            </view>
          </view>
        </view>
      </view>

      <view class="service-card cm-card cm-section">
        <text class="service-title">提示</text>
        <text class="service-text">如页面规则与门店公告存在差异，请以活动页与门店受理为准。</text>
      </view>
    </view>
  </view>
</template>

<script setup lang="ts">
import SectionHeader from '@/components/SectionHeader.vue'
import { useTheme } from '@/composables/use-theme'

const { themeClass } = useTheme()

function goBack() {
  uni.switchTab({ url: '/pages/profile/index' })
}

interface BriefItem {
  label: string
  value: string
}

interface RuleItem {
  index: string
  title: string
  text: string
}

interface ListRuleItem {
  title: string
  text: string
}

interface QaItem {
  q: string
  a: string
}

const briefItems: BriefItem[] = [
  { label: '适用门店', value: '按券面说明执行' },
  { label: '生效时间', value: '领取或支付成功后生效' },
  { label: '核销方式', value: '门店扫码或收银台受理' }
]

const applicableRules: RuleItem[] = [
  {
    index: '01',
    title: '门店范围',
    text: '适用门店、商品和服务项目以券面为准。'
  },
  {
    index: '02',
    title: '生效时点',
    text: '免费券领取后生效；付费券支付后发放。'
  },
  {
    index: '03',
    title: '有效期规则',
    text: '超过有效期后不可使用。'
  }
]

const usageRules: ListRuleItem[] = [
  {
    title: '领取与购买',
    text: '领取资格与购买限制以页面展示为准。'
  },
  {
    title: '到店核销',
    text: '到店出示可用券项，由门店扫码或收银受理。'
  },
  {
    title: '权益消耗顺序',
    text: '组合券包通常按券项独立核销。'
  },
  {
    title: '订单关联',
    text: '可在订单列表查看支付、发放与核销记录。'
  }
]

const invalidRules: string[] = [
  '超过有效期、已核销完或订单已关闭的权益不可继续使用。',
  '是否可叠加使用以券面说明为准。',
  '核销后不支持恢复。'
]

const qaItems: QaItem[] = [
  {
    q: '券包内多张券能否分次使用？',
    a: '如券面未特别说明，通常支持分次使用。'
  },
  {
    q: '门店未能及时展示记录怎么办？',
    a: '可稍后刷新，仍有差异可结合订单号向门店核对。'
  },
  {
    q: '免费券与付费券包是否共用规则？',
    a: '基础流程一致，具体以对应券面为准。'
  }
]
</script>

<style lang="scss" scoped>
.page-topbar {
  display: flex;
  justify-content: flex-start;
  margin-bottom: 18rpx;
}

.page-back {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-height: 56rpx;
  padding: 0 22rpx;
  border-radius: 999rpx;
  background: rgba(255, 252, 246, 0.86);
  border: 1rpx solid $cm-border-soft;
  color: $cm-text-primary;
  font-size: 24rpx;
}

.summary-card {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 18rpx;
  padding: 28rpx;
}

.summary-title {
  display: block;
  color: $cm-text-primary;
  font-size: 34rpx;
  font-weight: 700;
}

.summary-subtitle {
  display: block;
  margin-top: 10rpx;
  color: $cm-text-secondary;
  font-size: 24rpx;
  line-height: 1.8;
}

.summary-badge {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-height: 48rpx;
  padding: 0 18rpx;
  border-radius: 999rpx;
  background: rgba(183, 155, 99, 0.14);
  color: $cm-accent-gold;
  font-size: 22rpx;
}

.brief-card {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 16rpx;
  padding: 24rpx;
}

.brief-item {
  display: grid;
  gap: 8rpx;
  padding: 18rpx;
  border-radius: 24rpx;
  background: rgba(247, 242, 233, 0.76);
}

.brief-label {
  color: $cm-text-tertiary;
  font-size: 22rpx;
}

.brief-value {
  color: $cm-text-primary;
  font-size: 26rpx;
  font-weight: 600;
  line-height: 1.6;
}

.content-block {
  display: grid;
  gap: 20rpx;
  margin-top: 18rpx;
  padding: 28rpx;
}

.rule-row {
  display: grid;
  grid-template-columns: 72rpx 1fr;
  gap: 18rpx;
  align-items: start;
}

.rule-index {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 72rpx;
  border-radius: 24rpx;
  background: rgba(45, 91, 72, 0.1);
  color: $cm-primary;
  font-size: 24rpx;
  font-weight: 700;
}

.rule-body,
.list-item,
.qa-item {
  display: grid;
  gap: 10rpx;
}

.rule-title,
.list-title,
.panel-title,
.service-title,
.qa-question {
  color: $cm-text-primary;
  font-size: 30rpx;
  font-weight: 700;
}

.rule-text,
.list-text,
.panel-text,
.service-text,
.qa-answer {
  color: $cm-text-secondary;
  font-size: 24rpx;
  line-height: 1.8;
}

.two-column-section {
  display: grid;
  gap: 20rpx;
}

.faq-card,
.service-card {
  display: grid;
  gap: 16rpx;
  padding: 28rpx;
}

.panel-list,
.qa-list {
  display: grid;
  gap: 14rpx;
}

.panel-item {
  display: grid;
  grid-template-columns: 16rpx 1fr;
  gap: 12rpx;
  align-items: start;
}

.panel-dot {
  width: 12rpx;
  height: 12rpx;
  margin-top: 10rpx;
  border-radius: 50%;
  background: linear-gradient(135deg, #2d5b48 0%, #b79b63 100%);
}

.theme-light .page-back {
  background: #ffffff;
  border: 1rpx solid rgba(226, 232, 240, 0.9);
  color: #475569;
}

.theme-light .summary-badge {
  background: rgba(15, 23, 42, 0.06);
  color: #475569;
}

.theme-candy .page-back {
  background: #ffffff;
  border: 1rpx solid rgba(191, 219, 254, 0.6);
  color: #2563EB;
}

.theme-candy .summary-badge {
  background: rgba(59, 130, 246, 0.08);
  color: #2563EB;
}

.theme-orange .page-back {
  background: #ffffff;
  border: 1rpx solid rgba(254, 215, 170, 0.6);
  color: #EA580C;
}

.theme-orange .summary-badge {
  background: rgba(249, 115, 22, 0.08);
  color: #EA580C;
}

.theme-red .page-back {
  background: #ffffff;
  border: 1rpx solid rgba(255, 205, 210, 0.6);
  color: #E53935;
}

.theme-red .summary-badge {
  background: rgba(239, 83, 80, 0.08);
  color: #E53935;
}
</style>

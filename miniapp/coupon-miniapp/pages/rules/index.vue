<template>
  <view :class="['cm-page', 'cm-container', themeClass]">
    <view class="cm-nav-spacer"></view>

    <view class="rules-hero">
      <view class="rules-hero-mask"></view>
      <view class="rules-hero-content">
        <text class="rules-eyebrow">SERVICE GUIDE</text>
        <text class="rules-title">使用规则</text>
        <text class="rules-subtitle">领券、购券、核销前请先查看。</text>
      </view>
    </view>

    <view class="brief-card cm-card cm-section">
      <view class="brief-item" v-for="item in briefItems" :key="item.label">
        <text class="brief-label">{{ item.label }}</text>
        <text class="brief-value">{{ item.value }}</text>
      </view>
    </view>

    <view class="cm-section">
      <SectionHeader eyebrow="APPLICABLE" title="适用说明" subtitle="先看清使用范围" />
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
      <SectionHeader eyebrow="USAGE RULES" title="使用规则" subtitle="领取、下单、核销说明" />
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
      <text class="service-text">如页面规则与门店活动公告存在时间差异，请以活动有效期内的平台页面与门店受理信息为准。到店前可先确认营业时间与受理范围，以便更顺畅使用相关权益。</text>
    </view>
  </view>
</template>

<script setup lang="ts">
import SectionHeader from '@/components/SectionHeader.vue'
import { useTheme } from '@/composables/use-theme'

const { themeClass } = useTheme()

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
    text: '不同券项支持的门店范围可能不同，请以券面展示的适用门店、商品专区及服务项目为准。'
  },
  {
    index: '02',
    title: '生效时点',
    text: '免费领取券在领取成功后立即进入账户；付费券包在订单支付完成后发放对应权益。'
  },
  {
    index: '03',
    title: '有效期规则',
    text: '券项有效期按领取时间或活动规则计算，超过有效期后将无法继续使用。'
  }
]

const usageRules: ListRuleItem[] = [
  {
    title: '领取与购买',
    text: '部分权益面向新客、会员或特定活动开放，领取资格与购买限制以页面展示为准。'
  },
  {
    title: '到店核销',
    text: '到店后请主动出示可用券项，由门店完成扫码或收银受理，核销成功后即时更新状态。'
  },
  {
    title: '权益消耗顺序',
    text: '组合券包通常按券项独立核销，单次交易仅消耗符合条件的权益，不支持拆分退款。'
  },
  {
    title: '订单关联',
    text: '付费券包与订单一一对应，可通过订单列表查看支付信息、发放状态与后续核销记录。'
  }
]

const invalidRules: string[] = [
  '超过有效期、已全部核销完成或订单已关闭的权益不可继续使用。',
  '券项一般不与店内其他折扣、特价商品或特殊活动叠加，具体以券面说明为准。',
  '已核销权益不支持恢复，请在核销前确认商品、服务项目与使用门店。'
]

const qaItems: QaItem[] = [
  {
    q: '券包内多张券能否分次使用？',
    a: '如券面未特别说明，组合券包内的不同券项通常支持在有效期内分次使用。'
  },
  {
    q: '门店未能及时展示记录怎么办？',
    a: '可稍后刷新页面再次查看，若仍有差异，请结合订单号与核销单号向门店核对。'
  },
  {
    q: '免费券与付费券包是否共用规则？',
    a: '基础受理流程一致，但适用范围、有效期和使用限制需分别以对应券面为准。'
  }
]
</script>

<style lang="scss" scoped>
.rules-hero {
  position: relative;
  overflow: hidden;
  min-height: 292rpx;
  border-radius: 36rpx;
  background: linear-gradient(135deg, #274b3a 0%, #4d6956 58%, #a28a5c 100%);
}

.rules-hero-mask {
  position: absolute;
  inset: 0;
  background:
    radial-gradient(circle at right top, rgba(255, 243, 214, 0.22) 0, rgba(255, 243, 214, 0) 36%),
    linear-gradient(180deg, rgba(255, 255, 255, 0.06) 0%, rgba(255, 255, 255, 0) 52%);
}

.rules-hero-content {
  position: relative;
  z-index: 1;
  display: grid;
  gap: 16rpx;
  padding: 34rpx;
  color: #fffaf3;
}

.rules-eyebrow {
  color: rgba(247, 235, 208, 0.82);
  font-size: 22rpx;
  letter-spacing: 3rpx;
}

.rules-title {
  font-size: 46rpx;
  font-weight: 700;
}

.rules-subtitle {
  max-width: 620rpx;
  color: rgba(255, 250, 243, 0.88);
  font-size: 25rpx;
  line-height: 1.8;
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
</style>

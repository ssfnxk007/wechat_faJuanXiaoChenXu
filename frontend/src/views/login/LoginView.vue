<template>
  <div class="login-page">
    <div class="login-card">
      <section class="login-visual">
        <div>
          <div class="badge info">发卷系统后台</div>
          <h1>零售发券后台管理系统</h1>
          <p>面向小程序发券、券包售卖、支付回调、用户券管理与 ERP 核销的统一后台入口。</p>
          <ul>
            <li>支持微信获取 OpenId 后自动生成用户</li>
            <li>支持新人券、无门槛券、指定商品券、满减券</li>
            <li>支持公开领取、指定发放、券包售卖和支付发券</li>
            <li>支持生成券码二维码供 ERP 扫码核销</li>
          </ul>
        </div>

        <div class="login-metrics">
          <div class="login-metric">
            <span class="label">券类型</span>
            <strong>4 类</strong>
          </div>
          <div class="login-metric">
            <span class="label">发放方式</span>
            <strong>3 种</strong>
          </div>
          <div class="login-metric">
            <span class="label">核销模式</span>
            <strong>ERP 扫码</strong>
          </div>
        </div>
      </section>

      <section class="login-panel">
        <div class="login-panel-head">
          <div class="badge success">欢迎登录</div>
          <h1>进入后台</h1>
          <p>当前已接通后台 JWT 登录，先用开发管理员账号进入。</p>
        </div>

        <form class="login-form" @submit.prevent="goDashboard">
          <input v-model="form.username" type="text" placeholder="请输入账号" />
          <input v-model="form.password" type="password" placeholder="请输入密码" />
          <button type="submit">进入后台</button>
        </form>

        <div class="login-note">
          默认开发账号：`admin` / `123456`。后续可继续切换到数据库权限体系与动态菜单。
        </div>

        <p v-if="errorMessage" class="error-text">{{ errorMessage }}</p>
      </section>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { adminLogin, getAdminProfile } from '@/api/admin-auth'
import { authStorage } from '@/utils.auth'
import { getErrorMessage } from '@/utils/http-error'
import { notify } from '@/utils/notify'

const router = useRouter()
const errorMessage = ref('')
const form = reactive({
  username: 'admin',
  password: '123456',
})

const goDashboard = async () => {
  try {
    errorMessage.value = ''
    const response = await adminLogin({ ...form })
    authStorage.setAccessToken(response.data.accessToken)
    authStorage.setUsername(response.data.username)

    const profile = await getAdminProfile()
    authStorage.setDisplayName(profile.data.displayName || profile.data.username)
    authStorage.setMenuPaths(profile.data.menuPaths || [])
    authStorage.setPermissionCodes(profile.data.permissionCodes || [])

    notify.success('???????????')
    await router.push('/')
  } catch (error) {
    console.warn('[login:adminLogin]', error)
    errorMessage.value = getErrorMessage(error, '????????????')
    notify.error(errorMessage.value)
  }
}
</script>

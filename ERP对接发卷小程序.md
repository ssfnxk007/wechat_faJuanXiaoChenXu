# ERP 对接发卷小程序

## 目的

本文档说明 ERP 如何与发卷小程序后端对接核销链路。

适用场景：

- 门店收银 / POS 扫码核销
- ERP 先预检券，再完成交易，最后回调核销
- 商品券需要在核销时指定实际兑换商品

不适用场景：

- 小程序前端直接调用 ERP 核销接口
- 后台人工核销页面的操作说明

## 总体链路

1. 用户在小程序领取或购买后，后端生成 `UserCoupon`
2. 小程序展示券码 / 二维码
3. 门店 ERP 扫码拿到 `couponCode`
4. ERP 调用预检接口确认券是否可核销
5. ERP 完成实际结算 / 出库
6. ERP 调用正式核销接口
7. 后端把券状态改为已核销，并写入 `CouponWriteOffRecord`
8. 小程序“券详情 / 核销记录”页展示真实核销结果

## 对接前配置

后端配置节点：

```json
"ErpApi": {
  "ApiKey": "FaJuan-Erp-Key-2026-ChangeMe",
  "ApiKeyHeaderName": "X-Api-Key"
}
```

说明：

- `ApiKey`：ERP 调用专用密钥
- `ApiKeyHeaderName`：默认 `X-Api-Key`
- 两个 ERP 接口都受该 Header 保护

代码位置：

- [ErpApiOptions.cs](g:/发卷小程序/backend/src/FaJuan.Api/Contracts/ErpApiOptions.cs:3)
- [ErpApiKeyAuthorizeFilter.cs](g:/发卷小程序/backend/src/FaJuan.Api/Infrastructure/Auth/ErpApiKeyAuthorizeFilter.cs:19)

## 门店匹配规则

ERP 传入的 `siteCode` 会匹配后端门店表的 `Store.Code`。

要求：

- ERP 的门店编号必须和后端 `Store.Code` 保持一致
- 门店必须是启用状态

否则会返回：

- `门店不存在或已停用`

代码位置：

- [ErpCouponService.cs](g:/发卷小程序/backend/src/FaJuan.Api/Application/Erp/ErpCouponService.cs:15)
- [ErpCouponService.cs](g:/发卷小程序/backend/src/FaJuan.Api/Application/Erp/ErpCouponService.cs:75)

## 接口一：预检

### 请求

- 方法：`POST /api/erp/coupons/preview`
- Header：`X-Api-Key: <ERP密钥>`

请求体：

```json
{
  "siteCode": "S001",
  "couponCode": "CPN202604220001"
}
```

字段说明：

- `siteCode`：ERP 门店编号，对应后端 `Store.Code`
- `couponCode`：用户券券码

### 用途

预检只做校验，不修改状态。

ERP 应该用它来判断：

- 券是否存在
- 是否已核销 / 已作废 / 已过期
- 是否到了生效时间
- 当前门店是否在适用范围内
- 如果是商品券，是否存在商品适用范围

### 成功响应示例

```json
{
  "code": 200,
  "message": "可核销",
  "data": {
    "siteCode": "S001",
    "storeId": 1,
    "storeName": "南京东路店",
    "couponCode": "CPN202604220001",
    "userCouponId": 1001,
    "appUserId": 2001,
    "couponTemplateId": 3001,
    "couponTemplateName": "满40减5",
    "templateType": 4,
    "status": 1,
    "settlementType": "discount",
    "discountAmount": 5,
    "thresholdAmount": 40,
    "effectiveAt": "2026-04-22T10:00:00",
    "expireAt": "2026-05-22T23:59:59",
    "canWriteOff": true,
    "message": "可核销",
    "productScope": []
  },
  "success": true
}
```

### 核心字段说明

- `canWriteOff`
  - `true`：当前可以核销
  - `false`：不能核销，原因看 `message`
- `templateType`
  - `2`：无门槛券
  - `3`：商品券
  - `4`：满减券
- `settlementType`
  - `discount`：金额优惠券
  - `product_redeem`：商品兑换券
- `productScope`
  - 商品券时返回可兑换商品列表
  - 多商品券必须由 ERP 选择最终商品

代码位置：

- [ErpCouponsController.cs](g:/发卷小程序/backend/src/FaJuan.Api/Controllers/ErpCouponsController.cs:11)
- [ErpCouponService.cs](g:/发卷小程序/backend/src/FaJuan.Api/Application/Erp/ErpCouponService.cs:9)

## 接口二：正式核销

### 请求

- 方法：`POST /api/erp/coupons/writeoff`
- Header：`X-Api-Key: <ERP密钥>`

请求体：

```json
{
  "siteCode": "S001",
  "couponCode": "CPN202604220001",
  "selectedProductCode": "ERP001",
  "operatorName": "张三",
  "deviceCode": "POS-01"
}
```

字段说明：

- `siteCode`：ERP 门店编号
- `couponCode`：券码
- `selectedProductCode`：
  - 普通优惠券可不传
  - 商品券如果只配置一个商品，可以不传，后端自动选中
  - 商品券如果配置多个商品，必须传
- `operatorName`：经办人，可选
- `deviceCode`：设备号，可选

### 调用时机

建议流程：

1. 先调用预检
2. 预检通过后，ERP 完成自己的交易流程
3. 交易成功后，再调用正式核销

不要在真正交易成功前直接核销。

### 成功响应示例

```json
{
  "code": 200,
  "message": "核销成功",
  "data": {
    "userCouponId": 1001,
    "couponCode": "CPN202604220001",
    "appUserId": 2001,
    "couponTemplateId": 3001,
    "couponTemplateName": "指定商品券",
    "siteCode": "S001",
    "storeId": 1,
    "storeName": "南京东路店",
    "settlementType": "product_redeem",
    "selectedProductCode": "ERP001",
    "selectedProductName": "洗护礼盒",
    "message": "核销成功"
  },
  "success": true
}
```

### 后端会做什么

核销成功后，后端会：

- 把 `UserCoupon.Status` 改成已核销
- 商品券额外把履约状态改成 `Fulfilled`
- 新增一条 `CouponWriteOffRecord`
- 小程序端后续可看到真实核销记录

代码位置：

- [ErpCouponsController.cs](g:/发卷小程序/backend/src/FaJuan.Api/Controllers/ErpCouponsController.cs:24)
- [ErpCouponService.cs](g:/发卷小程序/backend/src/FaJuan.Api/Application/Erp/ErpCouponService.cs:69)

## 商品券特殊规则

商品券是这条链路里最需要 ERP 配合的一类券。

规则如下：

- 如果商品券没有配置可兑换商品，不能核销
- 如果只配置了一个商品，ERP 可不传 `selectedProductCode`
- 如果配置了多个商品，ERP 必须传 `selectedProductCode`
- `selectedProductCode` 必须属于该券的商品适用范围

失败文案示例：

- `商品券未配置可兑换商品`
- `商品券核销时必须选择商品`
- `所选商品不在券适用范围内`

## 常见失败返回

### 鉴权失败

- `401 缺少 ERP API Key`
- `401 ERP API Key 无效`
- `503 ERP API Key 未配置`

### 业务失败

- `门店不存在或已停用`
- `券不存在`
- `券模板不存在或已删除`
- `券已核销`
- `券已作废`
- `券未到生效时间`
- `券已过期`
- `当前门店不在该券适用范围内`
- `商品券未配置可兑换商品`
- `商品券核销时必须选择商品`
- `所选商品不在券适用范围内`

说明：

- `券不存在` 可能返回 `404`
- 其它大多返回 `400`

## 建议的 ERP 对接方式

推荐 ERP 侧实现成如下伪流程：

```text
扫码券码
  -> 调 preview
  -> 若 canWriteOff=false，直接提示 message
  -> 若 canWriteOff=true：
       若 productScope.length > 1，要求收银员选择商品
       完成 ERP 自身结算 / 出库
       调 writeoff
       成功后落 ERP 单据完成态
       失败则提示并进入人工处理
```

## 建议保存的 ERP 侧日志字段

为了后续排障，ERP 至少应记录：

- `siteCode`
- `couponCode`
- `selectedProductCode`
- `operatorName`
- `deviceCode`
- `preview` 请求时间 / 返回报文
- `writeoff` 请求时间 / 返回报文
- ERP 本地交易单号 / 小票号

## 联调检查清单

### 基础准备

- 后端 `ErpApi.ApiKey` 已配置
- ERP 已按约定传 `X-Api-Key`
- `siteCode` 已与后端 `Store.Code` 对齐
- 门店状态为启用

### 金额券联调

1. 用一张未使用金额券调 `preview`
2. 确认 `canWriteOff=true`
3. ERP 完成结算
4. 调 `writeoff`
5. 确认返回 `核销成功`
6. 后台 / 小程序核销记录可见

### 商品券联调

1. 用商品券调 `preview`
2. 检查 `productScope`
3. 单商品场景测试不传 `selectedProductCode`
4. 多商品场景测试必须传 `selectedProductCode`
5. 故意传错商品编码，确认失败文案正确

## 相关代码

- ERP 控制器：[ErpCouponsController.cs](g:/发卷小程序/backend/src/FaJuan.Api/Controllers/ErpCouponsController.cs:7)
- ERP 服务：[ErpCouponService.cs](g:/发卷小程序/backend/src/FaJuan.Api/Application/Erp/ErpCouponService.cs:7)
- ERP DTO：[ErpCouponDtos.cs](g:/发卷小程序/backend/src/FaJuan.Api/Contracts/ErpCouponDtos.cs:3)
- ERP 鉴权：[ErpApiKeyAuthorizeFilter.cs](g:/发卷小程序/backend/src/FaJuan.Api/Infrastructure/Auth/ErpApiKeyAuthorizeFilter.cs:8)
- 旧版 API 说明：[2026-04-22-ERP专用核销API说明.md](g:/发卷小程序/Docs/api/2026-04-22-ERP专用核销API说明.md:1)

## 当前结论

当前仓库里的 ERP 核销能力已经具备：

- 预检
- 正式核销
- 门店校验
- 商品券商品范围校验
- 核销落库
- 小程序核销结果展示

如果后续还需要，我建议再补一份“ERP 请求示例 Postman 集合”。

## 2026-04-29 新增：商品直购提货券规则

- 商品现在支持“直接购买”，不再依赖匹配关联券/推荐券。
- 支付成功后，后端会生成一张 `CPN...` 提货券，仍进入“可用券”列表。
- 该提货券会绑定唯一商品：
  - `BoundProductId`
  - `BoundProductName`
  - `BoundErpProductCode`
- ERP 预检时：
  - 若是商品直购提货券，`productScope` 只会返回 1 个绑定商品。
- ERP 正式核销时：
  - 必须传 `selectedProductCode`
  - 且必须与券绑定商品完全一致，否则返回“该券只能核销指定商品”。
- 有效期规则：
  - 由商品后台配置
  - 支持“固定日期范围”或“购买后 N 天有效”
  - 到期统一按自然日截止到 `23:59:59`
- 若商品未配置提货券有效期，则后端直接拒绝商品直购下单。

---

## 2026-04-29 对接补充（商品直购提货券）

这一节是对 ERP 对接方最重要的新增说明，建议直接按本节落地。

### 1. 业务变化

现在小程序里的“商品”支持**直接购买**，不再依赖“先领券再购买”或“靠推荐券/关联券匹配”来兜底。

用户支付成功后，后端会自动生成一张可核销的 `CPN...` 提货券：

- 券进入“小程序 - 可用券”列表
- 券只能核销**绑定的那个商品**
- 这张券本身就是唯一核销凭证
- ERP 不需要再做商品匹配推断

### 2. ERP 需要理解的两类商品券

ERP 侧现在要区分两类“商品券”：

#### 2.1 普通指定商品券

特点：

- 来自后台券模板配置
- `productScope` 可能返回多个商品
- ERP 在正式核销前，可能需要让收银员选择最终核销商品

#### 2.2 商品直购提货券（本次新增重点）

特点：

- 来自“小程序商品直接购买”
- 支付成功后自动发券
- 券绑定唯一商品
- `productScope` 只会返回 **1 个商品**
- ERP 正式核销时，`selectedProductCode` 必须传，且必须与该绑定商品完全一致

### 3. 预检接口不变，但语义变了

接口仍然是：

- `POST /api/erp/coupons/preview`

请求体仍然是：

```json
{
  "siteCode": "S001",
  "couponCode": "CPN202604220001"
}
```

对商品直购提货券，ERP 需要重点看返回里的：

- `canWriteOff`
- `message`
- `settlementType`
- `productScope`
- `effectiveAt`
- `expireAt`

其中：

- `settlementType = "product_redeem"` 表示这是商品兑换/提货类券
- 如果是商品直购提货券，`productScope` 只会返回 1 条
- ERP 可以直接把这 1 条商品作为本券允许核销的唯一商品

建议 ERP 预检处理规则：

1. 先判断 `canWriteOff`
2. 再判断 `productScope.length`
3. 如果 `productScope.length === 1`，默认这是“单商品提货券”处理模式
4. 不要再去做额外的商品推荐匹配或模糊比对

### 4. 正式核销接口要求更严格

接口仍然是：

- `POST /api/erp/coupons/writeoff`

请求体：

```json
{
  "siteCode": "S001",
  "couponCode": "CPN202604220001",
  "selectedProductCode": "ERP001",
  "operatorName": "张三",
  "deviceCode": "POS-01"
}
```

#### 4.1 商品直购提货券的必填要求

对于商品直购提货券：

- `selectedProductCode` **必填**
- 必须等于该券绑定商品的 `ErpProductCode`
- 不允许传别的商品编码

如果 ERP 不传 `selectedProductCode`，后端会返回：

- `商品提货券核销时必须提供商品编码`

如果 ERP 传了错误的商品编码，后端会返回：

- `该券只能核销指定商品`

#### 4.2 返回值里可直接落库的字段

正式核销成功后，建议 ERP 记录这些字段：

- `couponCode`
- `userCouponId`
- `couponTemplateId`
- `couponTemplateName`
- `siteCode`
- `storeId`
- `storeName`
- `settlementType`
- `productId`
- `selectedProductCode`
- `selectedProductName`
- `operatorName`
- `deviceCode`
- ERP 本地订单号 / 小票号

### 5. 商品直购提货券的到期规则

这次新增的商品直购提货券，有两种有效期配置方式：

#### 5.1 固定日期范围

由后台商品配置：

- `DirectPurchaseValidFrom`
- `DirectPurchaseValidTo`

到期时间统一按结束当天：

- `23:59:59`

例如：

- 开始：`2026-05-01`
- 结束：`2026-05-07`
- 实际到期时间：`2026-05-07 23:59:59`

#### 5.2 购买后 N 天有效

由后台商品配置：

- `DirectPurchaseValidDays`

注意：

- 不是“几点买就到几点”
- 而是按**自然日截止**
- 到期时间统一算到最后一天的 `23:59:59`

例如：

- 用户在 `2026-05-01 10:23:11` 购买
- 配置 `购买后 7 天有效`
- 实际到期时间为：`2026-05-08 23:59:59`

### 6. ERP 不要做的事情

ERP 对接时，请不要再做下面这些旧思路：

- 不要依赖“推荐券/可用券”去反推商品能不能买
- 不要把“商品页显示了券”理解成“必须先领券才能买”
- 不要对商品直购提货券做二次匹配推断
- 不要在正式核销时省略 `selectedProductCode`
- 不要把别的商品编码传给单商品提货券

### 7. 新增/重点失败文案

ERP 对接时，以下错误文案要重点处理：

- `门店不存在或已停用`
- `券不存在`
- `券模板不存在或已删除`
- `券已核销`
- `券已作废`
- `券未到生效时间`
- `券已过期`
- `当前门店不在该券适用范围内`
- `商品提货券缺少绑定商品`
- `商品券未配置可兑换商品`
- `商品券核销时必须选择商品`
- `商品提货券核销时必须提供商品编码`
- `所选商品不在券适用范围内`
- `该券只能核销指定商品`

建议 ERP 侧把这些 message 原样展示给收银员或记录到日志，便于联调排障。

### 8. ERP 联调建议流程（商品直购提货券）

建议至少验证以下场景：

#### 场景 A：正常核销

1. 小程序购买一个商品
2. 支付成功后拿到 `CPN...` 券码
3. ERP 调 `preview`
4. 确认：
   - `canWriteOff = true`
   - `settlementType = "product_redeem"`
   - `productScope.length = 1`
5. ERP 调 `writeoff`，并传正确的 `selectedProductCode`
6. 确认返回 `核销成功`

#### 场景 B：不传商品编码

1. 对商品直购提货券调用 `writeoff`
2. 不传 `selectedProductCode`
3. 应返回：`商品提货券核销时必须提供商品编码`

#### 场景 C：传错商品编码

1. 对商品直购提货券调用 `writeoff`
2. 传一个错误的 `selectedProductCode`
3. 应返回：`该券只能核销指定商品`

#### 场景 D：过期券

1. 准备一张已过期商品提货券
2. 调 `preview`
3. 应返回不可核销，且 `message = 券已过期`

### 9. 对接结论

对 ERP 来说，这次最重要的变化只有一句话：

> 商品直购后发放的 `CPN...` 提货券，就是唯一核销凭证，并且只能核销绑定商品；ERP 正式核销时必须传正确的 `selectedProductCode`。

如果 ERP 直接按这个规则实现，联调会最稳。

---

## 2026-04-30 现网修正说明（请 ERP 以本节为准）

### 1. 金额券识别不要再依赖 `templateType=4`

实测现网券码：

- `CPN20260422145831636F9DCA700`
- `CPN20260411223922779A902AD`

返回结果均为：

- `templateType = 1`
- `settlementType = "discount"`

因此，**ERP 当前判断“金额券/优惠券”时，应以 `settlementType = "discount"` 为准，不要再以 `templateType = 4` 作为唯一判断条件。**

当前建议识别规则：

- `settlementType = "discount"`：按金额券处理
- `settlementType = "product_redeem"`：按商品兑换/提货券处理

补充说明：

- `templateType` 仍然可以作为后台展示或辅助参考字段
- 但在现网对接里，`templateType` **不能作为 ERP 结算分支的主判断条件**

### 2. `productScope` 当前后端实际结构

当前后端 `preview` 接口返回的 `productScope`，实际 DTO 结构为：

```json
{
  "productId": 123,
  "productName": "超越训练",
  "erpProductCode": "75539620160078800040",
  "erpPrice": 78.8,
  "couponPrice": 48.0,
  "settlementPrice": 48.0
}
```

也就是说，当前后端明确返回的字段为：

- `productId`：内部商品 Id
- `productName`：商品名称
- `erpProductCode`：ERP 商品唯一码 / ERP 商品编码
- `erpPrice`：ERP 售价，对应商品档案中的 `ERP 售价`
- `couponPrice`：券价 / 小程序侧商品成交价，对应商品档案中的 `销售价格`
- `settlementPrice`：当前 ERP 结算建议价，现阶段与 `couponPrice` 保持一致

### 3. 关于价格字段的对接口径

如果 POS 当前实现像 `FaJuanApiClient.cs` 一样，预检成功链路里**严格要求**下面这些字段至少存在其一：

- `erpPrice`
- `couponPrice`
- `settlementPrice`

那么现在可以直接按现网接口对接：

- 商品原价 / ERP 定价：取 `erpPrice`
- 券价 / 小程序展示价：取 `couponPrice`
- 最终结算参考价：取 `settlementPrice`

当前实现下：

- `couponPrice = settlementPrice = 商品销售价格`

如果后续业务需要区分“展示价”和“核销结算价”，再单独扩展字段语义。

### 4. `productScope.length === 1` 只能作为“处理提示”，不能作为“来源识别”

文档上一版写过：

- `productScope.length === 1` 时，可按单商品提货券模式处理

这条作为 ERP 的处理建议是安全的，但它**不是严格识别条件**。

原因是：

- 普通“指定商品券”如果后台只配置了 1 个商品
- 那它的 `productScope.length` 也同样可能等于 `1`

所以现阶段正确理解应为：

- `productScope.length === 1`：说明当前券只允许核销 1 个商品，ERP 可以按单商品处理
- 但**不能仅凭这一点断定它一定是“商品直购提货券”来源**

### 5. 如果 ERP 需要严格区分“普通单商品券”和“商品直购提货券”

当前接口里，ERP **没有一个显式布尔标志位** 可以直接区分这两类来源。

如果后续 ERP 侧必须做严格分流，建议后端新增显式字段，例如任选一种：

- `sourceType`
- `isProductDirectPurchase`
- `couponSourceType`

在后端未补此字段前，ERP 不应把 `productScope.length === 1` 当作来源判定条件。

### 6. 本节结论

请 ERP 先按下面这套口径落地：

- **金额券识别看 `settlementType`，不要主看 `templateType`**
- **`productScope` 当前已返回 `productId/productName/erpProductCode/erpPrice/couponPrice/settlementPrice`**
- **`productScope.length === 1` 只能表示“只允许核销一个商品”，不能严格表示“这一定是商品直购提货券”**
- **如果 POS 必须依赖价格字段或必须严格识别券来源，需要单独补接口字段，不能靠文档推断**

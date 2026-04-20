// 把所有 .theme-candy 规则复制一份改为 .theme-orange / .theme-red
// 只替换 candy 规则块内的颜色，不影响其他样式
const fs = require('fs');
const path = require('path');

const root = 'g:/发卷小程序/miniapp/coupon-miniapp';
const files = [
  'pages/index/index.vue',
  'pages/profile/index.vue',
  'pages/activity/detail.vue',
  'pages/product/detail.vue',
  'pages/coupon-pack/detail.vue',
  'pages/coupon/detail.vue',
  'pages/mall/index.vue',
  'pages/order/result.vue',
  'pages/coupon/index.vue',
  'pages/order/list.vue',
];

// candy 色值 → orange 色值
const candyToOrange = [
  // hex（按长度倒序，防止 '#2563EB' 被 '#2563' 截断；这里没此问题但习惯）
  ['#1E3A8A', '#9A3412'],
  ['#1E40AF', '#C2410C'],
  ['#2563EB', '#EA580C'],
  ['#0EA5E9', '#EA580C'],
  ['#38BDF8', '#FB923C'],
  ['#3B82F6', '#F97316'],
  ['#60A5FA', '#FB923C'],
  ['#BFDBFE', '#FED7AA'],
  ['#DBEAFE', '#FFEDD5'],
  ['#E0E7FF', '#FFF7ED'],
  ['#EFF6FF', '#FFFBF5'],
  ['#F8FAFC', '#FFFBF5'],
  // rgba 主色（带空格 / 无空格都处理）
  ['rgba(59, 130, 246', 'rgba(249, 115, 22'],
  ['rgba(59,130,246',   'rgba(249,115,22'],
  ['rgba(37, 99, 235',  'rgba(234, 88, 12'],
  ['rgba(37,99,235',    'rgba(234,88,12'],
  ['rgba(219, 234, 254', 'rgba(255, 237, 213'],
  ['rgba(219,234,254',   'rgba(255,237,213'],
  ['rgba(191, 219, 254', 'rgba(254, 215, 170'],
  ['rgba(191,219,254',   'rgba(254,215,170'],
];

// candy 色值 → red 色值
const candyToRed = [
  ['#1E3A8A', '#B71C1C'],
  ['#1E40AF', '#C62828'],
  ['#2563EB', '#E53935'],
  ['#0EA5E9', '#E53935'],
  ['#38BDF8', '#F48080'],
  ['#3B82F6', '#EF5350'],
  ['#60A5FA', '#F48080'],
  ['#BFDBFE', '#FFCDD2'],
  ['#DBEAFE', '#FFCDD2'],
  ['#E0E7FF', '#FFEBEE'],
  ['#EFF6FF', '#FFEBEE'],
  ['#F8FAFC', '#FFFBFA'],
  ['rgba(59, 130, 246', 'rgba(239, 83, 80'],
  ['rgba(59,130,246',   'rgba(239,83,80'],
  ['rgba(37, 99, 235',  'rgba(229, 57, 53'],
  ['rgba(37,99,235',    'rgba(229,57,53'],
  ['rgba(219, 234, 254', 'rgba(255, 235, 238'],
  ['rgba(219,234,254',   'rgba(255,235,238'],
  ['rgba(191, 219, 254', 'rgba(255, 205, 210'],
  ['rgba(191,219,254',   'rgba(255,205,210'],
];

function applyMap(text, map) {
  let result = text;
  for (const [from, to] of map) {
    // 大小写不敏感的 replaceAll
    const re = new RegExp(from.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'), 'gi');
    result = result.replace(re, to);
  }
  return result;
}

let total = { files: 0, rules: 0 };

for (const rel of files) {
  const full = path.join(root, rel);
  let content = fs.readFileSync(full, 'utf8');
  const before = content;

  // 幂等：已经加过 orange 就跳过
  if (content.match(/\.theme-orange\s[^{]*\{/)) {
    console.log(`SKIP  ${rel}  (已有 .theme-orange)`);
    continue;
  }

  // 抓取所有 `.theme-candy <selector> { ... }` 规则块
  // 规则里不会包含嵌套 {}，所以 `[^}]*` 安全
  const regex = /\.theme-candy(?:\s|[^{]*?)\{[^}]*\}/g;
  const rules = content.match(regex) || [];
  if (rules.length === 0) {
    console.log(`SKIP  ${rel}  (没有 candy 规则)`);
    continue;
  }

  // 生成 orange + red 版本（替换前缀 + 替换色值）
  const toOrange = rules.map(r =>
    applyMap(r.replace(/\.theme-candy/g, '.theme-orange'), candyToOrange)
  );
  const toRed = rules.map(r =>
    applyMap(r.replace(/\.theme-candy/g, '.theme-red'), candyToRed)
  );

  const block =
    '\n/* ========== Orange Theme ========== */\n' +
    toOrange.join('\n\n') +
    '\n\n/* ========== Red Theme ========== */\n' +
    toRed.join('\n\n') +
    '\n';

  // 插入到最后一个 </style> 之前
  const idx = content.lastIndexOf('</style>');
  if (idx === -1) {
    console.log(`SKIP  ${rel}  (未找到 </style>)`);
    continue;
  }

  content = content.slice(0, idx) + block + content.slice(idx);

  fs.writeFileSync(full, content, 'utf8');
  console.log(`PATCH ${rel}  (复制 ${rules.length} 条 candy 规则)`);
  total.files += 1;
  total.rules += rules.length;
}

console.log(`\nDONE  修改 ${total.files} 个文件，共复制 ${total.rules} 条规则 × 2 主题`);

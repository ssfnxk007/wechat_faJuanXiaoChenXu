// 二次修补：只在 .theme-orange 和 .theme-red 段里补替换漏掉的 hex 色
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

// 漏掉的 blue-300
const orangePatch = [['#93C5FD', '#FDBA74']];
const redPatch = [['#93C5FD', '#FECDD3']];

function applyMap(text, map) {
  let result = text;
  for (const [from, to] of map) {
    const re = new RegExp(from.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'), 'gi');
    result = result.replace(re, to);
  }
  return result;
}

let total = 0;

for (const rel of files) {
  const full = path.join(root, rel);
  let content = fs.readFileSync(full, 'utf8');

  const orangeStart = content.indexOf('========== Orange Theme ==========');
  const redStart = content.indexOf('========== Red Theme ==========');
  const styleEnd = content.lastIndexOf('</style>');

  if (orangeStart === -1 || redStart === -1 || styleEnd === -1) {
    console.log(`SKIP  ${rel}`);
    continue;
  }

  const orangeBlock = content.slice(orangeStart, redStart);
  const redBlock = content.slice(redStart, styleEnd);

  const newOrange = applyMap(orangeBlock, orangePatch);
  const newRed = applyMap(redBlock, redPatch);

  const changed = (newOrange !== orangeBlock) || (newRed !== redBlock);

  if (!changed) {
    console.log(`NOOP  ${rel}`);
    continue;
  }

  content = content.slice(0, orangeStart) + newOrange + newRed + content.slice(styleEnd);
  fs.writeFileSync(full, content, 'utf8');
  console.log(`PATCH ${rel}`);
  total += 1;
}

console.log(`\nDONE  二次修补 ${total} 个文件`);

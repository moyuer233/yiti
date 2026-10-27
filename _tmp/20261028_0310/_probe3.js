
function log(s) { send({ t: 'log', m: String(s) }); }
function toUtf16(s) {
  const b = [];
  for (let i = 0; i < s.length; i++) { const c = s.charCodeAt(i); b.push(c & 0xff, (c >> 8) & 0xff); }
  return b;
}
function pattern(s) {
  const bytes = toUtf16(s);
  return bytes.map(x => ('0' + x.toString(16)).slice(-2)).join(' ') + ' 00 00';
}
const pats = {
  'ours  SegoeUIVariable-Regular': pattern('SegoeUIVariable-Regular'),
  'orig  SegoeUIVariable': pattern('SegoeUIVariable'),
  'yahei PS MicrosoftYaHei': pattern('MicrosoftYaHei'),
  'yahei UI UIps': pattern('MicrosoftYaHeiUI'),
};
const found = {};
for (const k in pats) found[k] = 0;
const ranges = Process.enumerateRanges('r--');
for (const r of ranges) {
  try {
    for (const k in pats) {
      let hits = 0;
      let cur = r.base;
      const end = r.base.add(r.size);
      while (hits < 3) {
        const m = Memory.scanSync(cur, end.sub(cur).toInt32 ? Number(end.sub(cur)) : 0, pats[k]);
        break;
      }
      void hits; // fallback below
    }
  } catch (e) { }
}
// 用 scanSync 的分块方式重做
for (const k in pats) {
  let total = 0;
  for (const r of ranges) {
    let offset = 0;
    const size = r.size;
    while (offset < size) {
      const chunk = Math.min(size - offset, 1024 * 1024);
      try {
        const res = Memory.scanSync(r.base.add(offset), chunk, pats[k]);
        total += res.length;
      } catch (e) { }
      offset += chunk;
      if (total > 20) break;
    }
    if (total > 20) break;
  }
  log(k + ' => ' + total + ' hits');
}
send({ t: 'done' });

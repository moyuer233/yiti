
function log(s) { send({ t: 'log', m: String(s) }); }
function utf16pat(s) {
  const out = [];
  for (let i = 0; i < s.length; i++) {
    const c = s.charCodeAt(i);
    out.push(('0' + (c & 0xff).toString(16)).slice(-2), ('0' + ((c >> 8) & 0xff).toString(16)).slice(-2));
  }
  return out.join(' ');
}
const pats = {
  'ORIG YaHei (Founder)': utf16pat('Founder'),
  'ORIG YaHei (6.31)': utf16pat('Version 6.31'),
  'HarmonyOS (Huawei)': utf16pat('Huawei'),
  'HarmonyOS word': utf16pat('HarmonyOS'),
  'MiSans word': utf16pat('MiSans'),
  'Segoe UIVariable-Regular': utf16pat('SegoeUIVariable-Regular'),
};
const ranges = Process.enumerateRanges('r--');
log('ranges=' + ranges.length);
for (const k in pats) {
  let total = 0;
  for (const r of ranges) {
    if (total > 3) break;
    if (r.size > 24 * 1024 * 1024) continue;
    try {
      const res = Memory.scanSync(r.base, r.size, pats[k]);
      total += res.length;
    } catch (e) { }
  }
  log(k + ' => ' + total);
}
send({ t: 'done' });

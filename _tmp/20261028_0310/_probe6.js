
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
  'OURS Version 4.003': utf16pat('Version 4.003'),
  'ORIG Version 6.31': utf16pat('Version 6.31'),
  'OURS Xiaomi': utf16pat('Xiaomi'),
};
const ranges = Process.enumerateRanges('r--');
for (const k in pats) {
  let total = 0;
  const addrs = [];
  for (const r of ranges) {
    if (total > 4) break;
    if (r.size > 32 * 1024 * 1024) continue;
    try {
      const res = Memory.scanSync(r.base, r.size, pats[k]);
      for (const x of res) { total++; if (addrs.length < 2) addrs.push(x.address.toString()); }
    } catch (e) { }
  }
  log(k + ' => ' + total + (addrs.length ? ' @ ' + addrs.join(',') : ''));
}
send({ t: 'done' });

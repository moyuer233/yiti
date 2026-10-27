
function log(s) { send({ t: 'log', m: String(s) }); }
function utf16pat(s) {
  let out = '';
  for (let i = 0; i < s.length; i++) {
    const c = s.charCodeAt(i);
    out += ('0' + (c & 0xff).toString(16)).slice(-2) + ' ' + ('0' + ((c >> 8) & 0xff).toString(16)).slice(-2) + ' ';
  }
  return out.trim();
}
const pats = {
  'OURS Xiaomi': utf16pat('Xiaomi'),
  'ORIG Founder': utf16pat('Founder'),
  'ORIG 6.31': utf16pat('Version 6.31'),
  'OURS SegoeUIVariable-Regular': utf16pat('SegoeUIVariable-Regular'),
};
const ranges = Process.enumerateRanges('r--');
log('readable ranges: ' + ranges.length);
for (const k in pats) {
  let total = 0;
  const addrs = [];
  for (const r of ranges) {
    if (total > 5) break;
    let offset = 0;
    const size = r.size;
    while (offset < size && total <= 5) {
      const chunk = Math.min(size - offset, 2 * 1024 * 1024);
      try {
        const res = Memory.scanSync(r.base.add(offset), chunk, pats[k]);
        for (const x of res) { total++; if (addrs.length < 3) addrs.push(x.address.toString()); }
      } catch (e) { }
      offset += chunk;
    }
  }
  log(k + ' => ' + total + (addrs.length ? '  at ' + addrs.join(',') : ''));
}
send({ t: 'done' });

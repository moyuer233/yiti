
function log(s) { send({ t: 'log', m: String(s) }); }
function utf16pat(s) {
  let out = [];
  for (let i = 0; i < s.length; i++) {
    const c = s.charCodeAt(i);
    out.push(('0' + (c & 0xff).toString(16)).slice(-2), ('0' + ((c >> 8) & 0xff).toString(16)).slice(-2));
  }
  return out.join(' ');
}
const pat = utf16pat('MicrosoftYaHei');
const ranges = Process.enumerateRanges('r--');
const hits = [];
for (const r of ranges) {
  if (hits.length >= 6) break;
  if (r.size > 64 * 1024 * 1024) continue;
  try {
    const res = Memory.scanSync(r.base, r.size, pat);
    for (const x of res) { hits.push(x.address); if (hits.length >= 6) break; }
  } catch (e) { }
}
log('hits=' + hits.length);
for (let i = 0; i < hits.length && i < 3; i++) {
  const a = hits[i];
  const start = a.sub(16);
  let hex = '';
  try {
    const buf = start.readByteArray(48);
    const u8 = new Uint8Array(buf);
    for (const b of u8) hex += ('0' + b.toString(16)).slice(-2);
  } catch (e) { hex = 'ERR ' + e.message; }
  log('hit' + i + ' @' + a + ' ctx=' + hex);
}
send({ t: 'done' });


function log(s) { send({ t: 'log', m: String(s) }); }
try {
  const ranges = Process.enumerateRanges('r--');
  log('ranges=' + ranges.length);
  const seen = {};
  for (const r of ranges) {
    if (r.file && r.file.path) {
      const p = r.file.path;
      const low = p.toLowerCase();
      if (low.indexOf('fonts') >= 0 || low.endsWith('.ttf') || low.endsWith('.ttc')) {
        seen[p] = (seen[p] || 0) + r.size;
      }
    }
  }
  const keys = Object.keys(seen);
  log('font-mapped files: ' + keys.length);
  for (const k of keys) log('  ' + k + '  bytes=' + seen[k]);
} catch (e) {
  log('ERR ' + e.message);
}
send({ t: 'done' });

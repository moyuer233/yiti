
function log(s) { send({ t: 'log', m: s }); }

const dwrite = Process.getModuleByName('dwrite.dll');
const createPtr = dwrite.getExportByName('DWriteCreateFactory');
const DWriteCreateFactory = new NativeFunction(createPtr, 'int', ['int', 'pointer', 'pointer']);

// IID_IDWriteFactory {b859ee5a-d838-4b5b-a2e8-1adc7d93db48}
const iid = Memory.alloc(16);
const bytes = [0x5a,0xee,0x59,0xb8, 0x38,0xd8, 0x5b,0x4b, 0xa2,0xe8, 0x1a,0xdc,0x7d,0x93,0xdb,0x48];
for (let i = 0; i < 16; i++) iid.add(i).writeU8(bytes[i]);

const ppv = Memory.alloc(Process.pointerSize);
const hr = DWriteCreateFactory(0, iid, ppv);
log('create factory hr=0x' + (hr >>> 0).toString(16));
if (hr !== 0) { send({ t: 'done' }); throw new Error('factory failed'); }
const factory = ppv.readPointer();

function vfn(obj, idx, ret, args) {
  const vtbl = obj.readPointer();
  const fp = vtbl.add(Process.pointerSize * idx).readPointer();
  return new NativeFunction(fp, ret, args);
}

// GetSystemFontCollection(this, IDWriteFontCollection**, BOOL)
const collOut = Memory.alloc(Process.pointerSize);
const hr2 = vfn(factory, 3, 'int', ['pointer','pointer','int'])(factory, collOut, 1);
log('GetSystemFontCollection hr=0x' + (hr2 >>> 0).toString(16));
const coll = collOut.readPointer();

const getCount = vfn(coll, 3, 'int', ['pointer','pointer']);
const cntOut = Memory.alloc(4);
getCount(coll, cntOut);
log('family count = ' + cntOut.readU32());

function findFamily(name) {
  const buf = Memory.alloc((name.length + 1) * 2);
  for (let i = 0; i < name.length; i++) buf.add(i * 2).writeU16(name.charCodeAt(i));
  buf.add(name.length * 2).writeU16(0);
  const idxOut = Memory.alloc(4);
  const existsOut = Memory.alloc(4);
  const hr = vfn(coll, 5, 'int', ['pointer','pointer','pointer','pointer'])(coll, buf, idxOut, existsOut);
  const exists = existsOut.readU32() !== 0;
  return { hr, exists, index: idxOut.readU32() };
}

function familyFile(name) {
  const r = findFamily(name);
  if (!r.exists) return null;
  const famOut = Memory.alloc(Process.pointerSize);
  vfn(coll, 4, 'int', ['pointer','uint','pointer'])(coll, r.index, famOut);
  const fam = famOut.readPointer();
  const fontOut = Memory.alloc(Process.pointerSize);
  // GetFirstMatchingFont(weight=400, stretch=5, style=0)
  const hr = vfn(fam, 7, 'int', ['pointer','int','int','int','pointer'])(fam, 400, 5, 0, fontOut);
  if (hr !== 0) return { err: 'firstMatching hr=0x' + (hr >>> 0).toString(16) };
  const font = fontOut.readPointer();
  const faceOut = Memory.alloc(Process.pointerSize);
  const hr2 = vfn(font, 13, 'int', ['pointer','pointer'])(font, faceOut);
  if (hr2 !== 0) return { err: 'createFace hr=0x' + (hr2 >>> 0).toString(16) };
  const face = faceOut.readPointer();
  const nOut = Memory.alloc(4);
  const hr3 = vfn(face, 4, 'int', ['pointer','pointer','pointer'])(face, nOut, ptr(0));
  if (hr3 !== 0) return { err: 'GetFiles(0) hr=0x' + (hr3 >>> 0).toString(16) };
  const n = nOut.readU32();
  let files = [];
  if (n > 0) {
    const arr = Memory.alloc(Process.pointerSize * n);
    const hr4 = vfn(face, 4, 'int', ['pointer','pointer','pointer'])(face, nOut, arr);
    if (hr4 === 0) {
      for (let i = 0; i < Math.min(n, 4); i++) {
        const file = arr.add(Process.pointerSize * i).readPointer();
        const sizeOut = Memory.alloc(4);
        const keyOut = Memory.alloc(Process.pointerSize);
        const hr5 = vfn(file, 3, 'int', ['pointer','pointer','pointer'])(file, sizeOut, keyOut);
        if (hr5 === 0) {
          const size = sizeOut.readU32();
          const keyPtr = keyOut.readPointer();
          let s = '';
          for (let k = 0; k + 1 < size && s.length < 260; k += 2) {
            const c = keyPtr.add(k).readU16();
            if (c === 0) break;
            s += String.fromCharCode(c);
          }
          files.push(s + '  (keySize=' + size + ')');
        }
      }
    }
  }
  return { files };
}

const names = ['Microsoft YaHei', 'Microsoft YaHei UI', 'Segoe UI Variable', 'Segoe UI'];
for (const nm of names) {
  try {
    const r = familyFile(nm);
    log(nm + ' => ' + JSON.stringify(r));
  } catch (e) {
    log(nm + ' => ERROR ' + e.message);
  }
}
send({ t: 'done' });

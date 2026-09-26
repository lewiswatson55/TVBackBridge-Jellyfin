const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

const listeners = new Map();
const labels = [];
let syntheticBackCount = 0;
const window = {
  addEventListener(type, callback) {
    const callbacks = listeners.get(type) || [];
    callbacks.push(callback);
    listeners.set(type, callbacks);
  },
  dispatchEvent(event) {
    if (event.key === 'Back') syntheticBackCount++;
    for (const callback of listeners.get(event.type) || []) callback(event);
  }
};
const document = {
  createElement() { return { textContent: '', style: {} }; },
  body: { appendChild(element) { labels.push(element); } }
};
class KeyboardEvent {
  constructor(type, init) {
    Object.assign(this, init, { type, keyCode: 0, isTrusted: false, target: window });
  }
}
const script = fs.readFileSync(path.join(process.cwd(), 'src', 'vidaa-back.js'), 'utf8')
  .replace('__VIDAA_BACK_OPTIONS__', JSON.stringify({ Enabled: true, DebugMode: true, BackKeyCode: 8, BackKeyName: 'Backspace' }));
vm.runInNewContext(script, { window, document, navigator: { userAgent: 'Toshiba VIDAA' }, KeyboardEvent });

const physicalBack = {
  type: 'keydown', key: 'Backspace', keyCode: 8, isTrusted: true, target: window,
  repeat: false, preventDefault() {}, stopImmediatePropagation() {}
};
window.dispatchEvent(physicalBack);
assert.equal(syntheticBackCount, 1);
assert.equal(labels[0].textContent, 'Key: Backspace (8)');
console.log('Key viewer test passed: remote key remains visible after synthetic Back.');

const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

const template = fs.readFileSync(path.join(process.cwd(), 'src', 'vidaa-back.js'), 'utf8');

function runScenario(userAgent, keywords, hisenseApi = false) {
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
  if (hisenseApi) window.Hisense_GetFirmWareVersion = () => 'test';
  const document = {
    createElement() { return { textContent: '', style: {} }; },
    body: { appendChild(element) { labels.push(element); } }
  };
  class KeyboardEvent {
    constructor(type, init) {
      Object.assign(this, init, { type, keyCode: 0, isTrusted: false, target: window });
    }
  }
  const options = { Enabled: true, DebugMode: true, BackKeyCode: 8, BackKeyName: 'Backspace', UserAgentKeywords: keywords };
  const script = template.replace('__VIDAA_BACK_OPTIONS__', JSON.stringify(options));
  vm.runInNewContext(script, { window, document, navigator: { userAgent }, KeyboardEvent });

  const physicalBack = {
    type: 'keydown', key: 'Backspace', keyCode: 8, isTrusted: true, target: window,
    repeat: false, preventDefault() {}, stopImmediatePropagation() {}
  };
  window.dispatchEvent(physicalBack);
  return { syntheticBackCount, labels };
}

const defaults = 'vidaa, hisense, toshiba';
const toshiba = runScenario('Toshiba VIDAA', defaults);
assert.equal(toshiba.syntheticBackCount, 1);
assert.equal(toshiba.labels[0].textContent, 'Key: Backspace (8)');
assert.equal(runScenario('Desktop Chrome', defaults).syntheticBackCount, 0);
assert.equal(runScenario('Unknown TV', defaults, true).syntheticBackCount, 1);
assert.equal(runScenario('Brand X Model', 'brand x').syntheticBackCount, 1);
assert.equal(runScenario('Desktop Chrome', 'brand x').syntheticBackCount, 0);
assert.equal(runScenario('Desktop Chrome', '').syntheticBackCount, 1);
console.log('User-agent and key-viewer tests passed.');

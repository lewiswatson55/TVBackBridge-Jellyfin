(function () {
  /*
   * Edit these values for your remote. The defaults are from my tested Toshiba VIDAA TV but are likely the default so try this first.
   * Set SHOW_KEY_VIEWER to true to see the key name and number on the TV.
   * Turn it off again after finding your Back key.
   */
  var BACK_KEY_CODE = 8;
  var BACK_KEY_NAME = 'Backspace';
  var SHOW_KEY_VIEWER = false;

  var userAgent = navigator.userAgent.toLowerCase();
  var isVidaaTv = /vidaa|hisense|toshiba/.test(userAgent) ||
    typeof window.Hisense_GetFirmWareVersion === 'function';
  if (!isVidaaTv) return;

  var label;
  if (SHOW_KEY_VIEWER) {
    label = document.createElement('div');
    label.textContent = 'TV key viewer ready';
    label.style.cssText = 'position:fixed;bottom:12px;right:12px;z-index:2147483647;background:#111;color:white;padding:12px;font-size:20px;pointer-events:none';
    document.body.appendChild(label);
  }

  function isTextInput(target) {
    return target && (target.isContentEditable || target.tagName === 'INPUT' || target.tagName === 'TEXTAREA');
  }

  function isBackKey(event) {
    return event.isTrusted && (
      (BACK_KEY_CODE > 0 && event.keyCode === BACK_KEY_CODE) ||
      (BACK_KEY_NAME && event.key === BACK_KEY_NAME)
    );
  }

  window.addEventListener('keydown', function (event) {
    if (label && event.isTrusted) {
      label.textContent = 'Key: ' + event.key + ' (' + event.keyCode + ')';
    }
    if (!isBackKey(event) || isTextInput(event.target)) return;

    event.preventDefault();
    event.stopImmediatePropagation();
    if (event.repeat) return;

    window.dispatchEvent(new KeyboardEvent('keydown', {
      key: 'Back', code: 'Back', bubbles: true, cancelable: true
    }));
  }, true);

  window.addEventListener('keyup', function (event) {
    if (isBackKey(event) && !isTextInput(event.target)) {
      event.preventDefault();
      event.stopImmediatePropagation();
    }
  }, true);
})();

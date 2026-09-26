(function () {
  var options = __VIDAA_BACK_OPTIONS__;
  var userAgent = navigator.userAgent.toLowerCase();
  var keywordSetting = String(options.UserAgentKeywords == null ? 'vidaa, hisense, toshiba' : options.UserAgentKeywords);
  var keywords = keywordSetting.split(',').map(function (keyword) {
    return keyword.trim().toLowerCase();
  }).filter(Boolean);
  var useDefaultKeywords = keywords.length === 3 &&
    ['vidaa', 'hisense', 'toshiba'].every(function (keyword) { return keywords.indexOf(keyword) !== -1; });
  var isMatchingDevice = !keywordSetting.trim() || keywords.some(function (keyword) {
    return userAgent.indexOf(keyword) !== -1;
  }) || (useDefaultKeywords && typeof window.Hisense_GetFirmWareVersion === 'function');

  if (!isMatchingDevice) {
    return;
  }

  var label;
  if (options.DebugMode) {
    label = document.createElement('div');
    label.textContent = 'TV key debug ready';
    label.style.cssText = 'position:fixed;bottom:12px;right:12px;z-index:2147483647;background:#111;color:white;padding:12px;font-size:20px;pointer-events:none';
    document.body.appendChild(label);
  }

  var backKeyCode = Number(options.BackKeyCode);
  var backKeyName = String(options.BackKeyName || '');

  function isTextInput(target) {
    return target && (
      target.isContentEditable ||
      target.tagName === 'INPUT' ||
      target.tagName === 'TEXTAREA'
    );
  }

  function isMappedKey(event) {
    return event.isTrusted && (
      (backKeyCode > 0 && event.keyCode === backKeyCode) ||
      (backKeyName && event.key === backKeyName)
    );
  }

  window.addEventListener('keydown', function (event) {
    if (label && event.isTrusted) {
      label.textContent = 'Key: ' + event.key + ' (' + event.keyCode + ')';
    }

    if (!options.Enabled || !isMappedKey(event) || isTextInput(event.target)) {
      return;
    }

    event.preventDefault();
    event.stopImmediatePropagation();

    if (event.repeat) {
      return;
    }

    window.dispatchEvent(new KeyboardEvent('keydown', {
      key: 'Back',
      code: 'Back',
      bubbles: true,
      cancelable: true
    }));
  }, true);

  window.addEventListener('keyup', function (event) {
    if (options.Enabled && isMappedKey(event) && !isTextInput(event.target)) {
      event.preventDefault();
      event.stopImmediatePropagation();
    }
  }, true);
})();

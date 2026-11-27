mergeInto(LibraryManager.library, {
  TrackingLog_GetUserAgent: function () {
    var ua = '';
    try {
      if (typeof navigator !== 'undefined' && navigator && navigator.userAgent) {
        ua = String(navigator.userAgent);
      }
    } catch (e) {
      ua = '';
    }
    var len = lengthBytesUTF8(ua) + 1;
    var buf = _malloc(len);
    stringToUTF8(ua, buf, len);
    return buf;
  },

  TrackingLog_SendImmediate: function (urlPtr, bodyPtr, headersJsonPtr) {
    var readStr = typeof UTF8ToString === 'function' ? UTF8ToString : Pointer_stringify;
    var url = readStr(urlPtr);
    var body = readStr(bodyPtr);
    var headersJson = headersJsonPtr ? readStr(headersJsonPtr) : '';

    if (!url || !body) {
      return 0;
    }

    var customHeaders = null;
    var hasCustomHeaders = false;
    if (headersJson) {
      try {
        customHeaders = JSON.parse(headersJson);
        for (var k in customHeaders) {
          if (Object.prototype.hasOwnProperty.call(customHeaders, k)) {
            hasCustomHeaders = true;
            break;
          }
        }
      } catch (e) {
        customHeaders = null;
        hasCustomHeaders = false;
      }
    }

    // sendBeacon does not support custom Authorization/API-Key headers, so use fetch keepalive when headers exist.
    if (hasCustomHeaders && typeof fetch === 'function') {
      try {
        var fetchHeaders = { 'Content-Type': 'application/json' };
        for (var key in customHeaders) {
          if (Object.prototype.hasOwnProperty.call(customHeaders, key)) {
            fetchHeaders[key] = customHeaders[key];
          }
        }
        fetch(url, {
          method: 'POST',
          headers: fetchHeaders,
          body: body,
          keepalive: true
        }).catch(function () {});
        return 1;
      } catch (e) {}
    }

    if (typeof navigator !== 'undefined' && navigator && typeof navigator.sendBeacon === 'function') {
      try {
        var blob = new Blob([body], { type: 'application/json' });
        if (navigator.sendBeacon(url, blob)) {
          return 1;
        }
      } catch (e) {}
    }

    if (typeof fetch === 'function') {
      try {
        fetch(url, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: body,
          keepalive: true
        }).catch(function () {});
        return 1;
      } catch (e) {}
    }

    return 0;
  },

  TrackingLog_RegisterLifecycleHooks: function (goNamePtr, unloadMethodPtr, visibilityMethodPtr) {
    if (typeof window === 'undefined' || typeof document === 'undefined') {
      return;
    }

    var readStr = typeof UTF8ToString === 'function' ? UTF8ToString : Pointer_stringify;
    window.__trackingLogHookTarget = {
      goName: readStr(goNamePtr),
      unloadMethod: readStr(unloadMethodPtr),
      visibilityMethod: readStr(visibilityMethodPtr)
    };

    if (window.__trackingLogHooksRegistered) {
      return;
    }
    window.__trackingLogHooksRegistered = true;

    function dispatchToUnity(methodName, arg) {
      var target = window.__trackingLogHookTarget;
      if (!target || !target.goName || !methodName) {
        return;
      }
      try {
        if (typeof SendMessage === 'function') {
          SendMessage(target.goName, methodName, arg);
        } else if (typeof Module !== 'undefined' && Module && typeof Module.SendMessage === 'function') {
          Module.SendMessage(target.goName, methodName, arg);
        }
      } catch (e) {}
    }

    function onUnload() {
      var target = window.__trackingLogHookTarget;
      if (target) {
        dispatchToUnity(target.unloadMethod, '');
      }
    }

    window.addEventListener('pagehide', onUnload);
    window.addEventListener('beforeunload', onUnload);
    document.addEventListener('visibilitychange', function () {
      var target = window.__trackingLogHookTarget;
      if (target) {
        var state = document.hidden ? 'hidden' : 'visible';
        dispatchToUnity(target.visibilityMethod, state);
      }
    });
  }
});

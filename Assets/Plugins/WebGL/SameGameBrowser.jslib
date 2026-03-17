var SameGameBrowser = {
  currentBgm: null,
  currentBgmUrl: "",
  requestedBgmUrl: "",
  masterBgmVolume: 0.5,
  switchToken: 0,
  unlockHandlerRegistered: false,
  audioUnlocked: false,
  bgmFadeDurationMs: 450,

  clampVolume: function (value) {
    if (value < 0) {
      return 0;
    }

    if (value > 1) {
      return 1;
    }

    return value;
  },

  canUseFullscreen: function () {
    return !!(window.sameGameUnityInstance &&
      typeof window.sameGameUnityInstance.SetFullscreen === "function" &&
      (document.fullscreenEnabled ||
        document.webkitFullscreenEnabled ||
        document.msFullscreenEnabled));
  },

  isFullscreen: function () {
    return !!(document.fullscreenElement ||
      document.webkitFullscreenElement ||
      document.msFullscreenElement);
  },

  toggleFullscreen: function () {
    if (!this.canUseFullscreen()) {
      return;
    }

    window.sameGameUnityInstance.SetFullscreen(this.isFullscreen() ? 0 : 1);
  },

  ensureUnlockHandler: function () {
    if (this.unlockHandlerRegistered) {
      return;
    }

    this.unlockHandlerRegistered = true;
    var self = this;
    var unlock = function () {
      self.audioUnlocked = true;
      self.startRequestedBgm();
    };

    window.addEventListener("pointerdown", unlock, true);
    window.addEventListener("touchstart", unlock, true);
    window.addEventListener("keydown", unlock, true);
  },

  createLoopingAudio: function (url) {
    var audio = new Audio(url);
    audio.preload = "auto";
    audio.loop = true;
    audio.volume = 0;
    audio.crossOrigin = "anonymous";
    return audio;
  },

  startAudio: function (audio, onStarted) {
    if (!audio) {
      return;
    }

    var self = this;
    var playPromise = audio.play();
    if (playPromise && typeof playPromise.then === "function") {
      playPromise.then(function () {
        self.audioUnlocked = true;
        if (onStarted) {
          onStarted();
        }
      }).catch(function () {
        // Browser autoplay policy blocked playback. We keep the requested URL
        // and retry on the next user gesture.
      });
      return;
    }

    if (onStarted) {
      onStarted();
    }
  },

  fadeBetween: function (fromAudio, toAudio, token) {
    var self = this;
    var startTime = null;

    toAudio.volume = 0;
    this.startAudio(toAudio, function () {
      var step = function (timestamp) {
        if (token !== self.switchToken) {
          toAudio.pause();
          toAudio.currentTime = 0;
          return;
        }

        if (!startTime) {
          startTime = timestamp;
        }

        var elapsed = timestamp - startTime;
        var t = Math.min(1, elapsed / self.bgmFadeDurationMs);
        var targetVolume = self.masterBgmVolume;
        if (fromAudio) {
          fromAudio.volume = (1 - t) * targetVolume;
        }

        toAudio.volume = t * targetVolume;

        if (t < 1) {
          window.requestAnimationFrame(step);
          return;
        }

        if (fromAudio) {
          fromAudio.pause();
          fromAudio.currentTime = 0;
          fromAudio.volume = 0;
        }

        toAudio.volume = targetVolume;
        self.currentBgm = toAudio;
      };

      window.requestAnimationFrame(step);
    });
  },

  startRequestedBgm: function () {
    if (!this.requestedBgmUrl || !this.audioUnlocked) {
      return;
    }

    if (this.currentBgm && this.currentBgmUrl === this.requestedBgmUrl) {
      this.currentBgm.volume = this.masterBgmVolume;
      this.startAudio(this.currentBgm);
      return;
    }

    var nextAudio = this.createLoopingAudio(this.requestedBgmUrl);
    var previousAudio = this.currentBgm;
    this.currentBgmUrl = this.requestedBgmUrl;
    this.switchToken += 1;
    var token = this.switchToken;

    if (!previousAudio) {
      this.startAudio(nextAudio, function () {
        if (token !== SameGameBrowser.switchToken) {
          nextAudio.pause();
          nextAudio.currentTime = 0;
          return;
        }

        nextAudio.volume = SameGameBrowser.masterBgmVolume;
        SameGameBrowser.currentBgm = nextAudio;
      });
      return;
    }

    this.fadeBetween(previousAudio, nextAudio, token);
  },

  playBgm: function (url, volume) {
    this.masterBgmVolume = this.clampVolume(volume);
    this.requestedBgmUrl = url || "";
    this.ensureUnlockHandler();

    if (!this.requestedBgmUrl) {
      this.stopBgm();
      return;
    }

    this.startRequestedBgm();
  },

  setBgmVolume: function (volume) {
    this.masterBgmVolume = this.clampVolume(volume);
    if (this.currentBgm) {
      this.currentBgm.volume = this.masterBgmVolume;
    }
  },

  stopBgm: function () {
    this.requestedBgmUrl = "";
    this.currentBgmUrl = "";
    this.switchToken += 1;

    if (this.currentBgm) {
      this.currentBgm.pause();
      this.currentBgm.currentTime = 0;
      this.currentBgm.volume = 0;
      this.currentBgm = null;
    }
  }
};

mergeInto(LibraryManager.library, {
  $SameGameBrowser: SameGameBrowser,

  SameGame_PlayStreamingBgm: function (urlPtr, volume) {
    SameGameBrowser.playBgm(UTF8ToString(urlPtr), volume);
  },

  SameGame_SetStreamingBgmVolume: function (volume) {
    SameGameBrowser.setBgmVolume(volume);
  },

  SameGame_StopStreamingBgm: function () {
    SameGameBrowser.stopBgm();
  },

  SameGame_ToggleFullscreen: function () {
    SameGameBrowser.toggleFullscreen();
  },

  SameGame_IsFullscreen: function () {
    return SameGameBrowser.isFullscreen() ? 1 : 0;
  },

  SameGame_CanUseFullscreen: function () {
    return SameGameBrowser.canUseFullscreen() ? 1 : 0;
  }
});

autoAddDeps(LibraryManager.library, "$SameGameBrowser");

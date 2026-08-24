mergeInto(LibraryManager.library, {
  RegisterFullscreenExitListener: function (gameObjectNamePtr) {
    var goName = UTF8ToString(gameObjectNamePtr);
    document.addEventListener('fullscreenchange', function () {
      if (!document.fullscreenElement) {
        SendMessage(goName, 'OnBrowserFullscreenExited');
      }
    });
  }
});

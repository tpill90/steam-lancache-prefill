/*
    Example Usage:
        <div data-cli-player="../casts/docker-pull.cast" data-rows=25></div>

    Defaults:
        data-cli-rows - 14
*/
(function ()
{
    // Asciinema will throw a null exception when being loaded from the browser's cache.  Appears to be related to how quickly the .js library is loaded locally,
    // versus there being a longer delay when loading over the network.
    // Scheduling initPlayers() with setTimeout() adds just enough delay to fix the issue.
    window.addEventListener('load', function () { setTimeout(initPlayers, 0) }, false);

    // Default settings used by all Asciinema players
    const asciinemaOpts = {
        terminalFontSize: "15px",
        fit: 'none',
        terminalFontFamily: "'Cascadia Mono', monospace",
        cols: 120
    };

    const defaultObserverOpts = {
        threshold: .5
    };

    function initPlayers() 
    {
        let playerElements = document.querySelectorAll('[data-cli-player]');
        playerElements.forEach((playerElement) =>
        {
            // Collect needed properties from the player's DOM element attributes
            var asciiOpts = { ...asciinemaOpts };
            let castFileSource = playerElement.getAttribute('data-cli-player');
            asciiOpts.rows = playerElement.getAttribute('data-rows') ?? 14;

            let player = AsciinemaPlayer.create(castFileSource, playerElement, asciiOpts);

            // Use IntersectionObserver to trigger the player only when the user scrolls scrolled into the viewport
            let observer = new IntersectionObserver((entries) => 
            {
                if (entries[0].isIntersecting) 
                {
                    console.log("triggered");
                    player.play();
                    // Removing the observer so that the player doesn't restart if the user scrolls up/down
                    observer.unobserve(playerElement);
                }
            }, defaultObserverOpts);

            observer.observe(playerElement);
        });
    };
})();

// Creates a copy to clipboard button to be clicked on code snippets
$(document).ready(function() {
    // https://clipboardjs.com/
    var selectors = document.querySelectorAll('pre code');
    var copyButton = '<span class="clipboard"><span class="btn btn-neutral btn-clipboard" title="Copy to clipboard">Copy</span></span>';
    Array.prototype.forEach.call(selectors, function(selector){
      selector.insertAdjacentHTML('afterbegin', copyButton);
    });
    var clipboard = new ClipboardJS('.btn-clipboard', {
      target: function (trigger) {
        return trigger.parentNode.nextElementSibling;
      }
    });
  
    clipboard.on('success', function (e) {
      e.clearSelection();
  
      // https://atomiks.github.io/tippyjs/v6/all-props/
      var tippyInstance = tippy(
        e.trigger,
        {
          content: 'Copied',
          showOnCreate: true,
          trigger: 'manual',
        },
      );
      setTimeout(function() { tippyInstance.hide(); }, 1000);
    });
  });

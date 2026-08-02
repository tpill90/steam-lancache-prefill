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

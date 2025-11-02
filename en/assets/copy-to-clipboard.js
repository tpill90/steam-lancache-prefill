// Creates a "copy to clipboard" button on each code snippet, that can be clicked to copy the contents to clipboard
// Uses : https://clipboardjs.com/
$(document).ready(function ()
{
    // Adds a copy button to each code block
    var copyButton = '<span class="clipboardContainer"><span class="btn btn-neutral btn-clipboard" title="Copy to clipboard">Copy</span></span>';
    var selectors = document.querySelectorAll('pre code');
    Array.prototype.forEach.call(selectors, function (selector)
    {
        selector.insertAdjacentHTML('afterbegin', copyButton);
    });
    
    var clipboard = new ClipboardJS('.btn-clipboard', {
        text: function (trigger)
        {
            // Getting all text contained in the code block, this includes the "Copy" button
            let allCodeBlockLines = trigger.parentNode.parentNode.innerText;

            // Removing the "Copy" button text that will be inadvertently included since its part of the inner text
            return allCodeBlockLines.substring(allCodeBlockLines.indexOf("\n") + 1);
        }
    });

    // Shows the "Copied" tooltip when clicking the copy button
    clipboard.on('success', function (e)
    {
        e.clearSelection();

        var tippyInstance = tippy(
            e.trigger,
            {
                content: 'Copied',
                showOnCreate: true,
                trigger: 'manual',
            },
        );
        setTimeout(function () { tippyInstance.hide(); }, 1000);
    });
});
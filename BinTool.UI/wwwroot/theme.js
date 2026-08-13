// Light / dark theme.
//
// Loaded blocking from the <head> so the attribute is on <html> before the first paint.
// The stylesheet keys off `data-theme` on the root: "dark" and "light" force a theme,
// and the attribute being absent means "follow the operating system", which app.css
// handles with a prefers-color-scheme query. So the only state worth storing is an
// explicit choice - never the resolved value, or a user who picked nothing would be
// pinned to whatever their OS happened to be on the day they first loaded the page.
//
// A plain global rather than a Blazor interop module: the shell renders statically, so
// the button that calls it is a DOM handler with no circuit behind it.

window.BinToolTheme = (function () {
    var KEY = 'bintool-theme';
    var root = document.documentElement;

    // Private browsing and locked-down group policies can both make localStorage throw
    // on access rather than return null, which would take the whole script down with it.
    function stored() {
        try {
            return window.localStorage.getItem(KEY);
        } catch (e) {
            return null;
        }
    }

    function remember(theme) {
        try {
            window.localStorage.setItem(KEY, theme);
        } catch (e) {
            // A theme that does not survive the reload is still better than a broken page.
        }
    }

    function apply(theme) {
        if (theme === 'dark' || theme === 'light') {
            root.setAttribute('data-theme', theme);
        } else {
            root.removeAttribute('data-theme');
        }
    }

    // What the user is looking at right now, which is what a toggle has to move away
    // from - the stored choice alone does not answer this when nothing is stored.
    function current() {
        var chosen = root.getAttribute('data-theme');
        if (chosen === 'dark' || chosen === 'light') {
            return chosen;
        }

        return window.matchMedia &&
            window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    }

    apply(stored());

    // Enhanced navigation is the reason this is not a set-once script. When Blazor follows an
    // internal link it fetches the next page and morphs the live DOM to match it, and that morph
    // reaches <html>: the server response never carries `data-theme` - it is set here, in the
    // browser - so the attribute is stripped on every page change and the theme falls back to the
    // OS. The head script does not re-run to put it back. So re-apply after each enhanced load.
    //
    // `enhancedload` fires synchronously at the end of the DOM update, before the browser paints,
    // so restoring the attribute there is invisible rather than a flash of the wrong theme. Blazor
    // is not defined yet while the head is parsing, so the registration waits for it.
    function hookEnhancedNav() {
        if (window.Blazor && typeof window.Blazor.addEventListener === 'function') {
            window.Blazor.addEventListener('enhancedload', function () {
                apply(stored());
            });
        } else {
            setTimeout(hookEnhancedNav, 50);
        }
    }

    hookEnhancedNav();

    return {
        toggle: function () {
            var next = current() === 'dark' ? 'light' : 'dark';
            apply(next);
            remember(next);
        }
    };
})();

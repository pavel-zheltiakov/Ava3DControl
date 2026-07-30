// The story slider.
//
// Progressive enhancement rather than a component: the markup is eight <figure>s and the stylesheet shows all
// of them. This script adds a class that collapses them to one at a time and wires up the controls, so a
// reader with no JavaScript gets the same eight pictures and captions in the same order — just scrolled
// instead of clicked.
//
// No dependencies, no build step. The whole page is files a static host can serve.

(function () {
    'use strict';

    document.querySelectorAll('[data-slider]').forEach(function (slider) {
        var slides = Array.prototype.slice.call(slider.querySelectorAll('.slide'));
        if (slides.length < 2) return;

        var dots = slider.querySelector('[data-dots]');
        var prev = slider.querySelector('[data-prev]');
        var next = slider.querySelector('[data-next]');
        var index = 0;
        var buttons = [];

        slides.forEach(function (slide, i) {
            var heading = slide.querySelector('h3');
            var label = heading ? heading.textContent.trim() : 'Slide ' + (i + 1);

            var button = document.createElement('button');
            button.type = 'button';
            button.setAttribute('role', 'tab');
            // The dot shows its number; the accessible name is the slide's own heading, which is far more
            // use to a screen reader than "3 of 8".
            button.textContent = String(i + 1);
            button.setAttribute('aria-label', label);
            button.addEventListener('click', function () { show(i); });

            dots.appendChild(button);
            buttons.push(button);
        });

        function show(target) {
            index = (target + slides.length) % slides.length;

            slides.forEach(function (slide, i) {
                var current = i === index;
                slide.classList.toggle('current', current);
                // Hidden slides are removed from the accessibility tree as well as from the layout, so a
                // screen reader is not read all eight captions in a row.
                slide.setAttribute('aria-hidden', current ? 'false' : 'true');
            });

            buttons.forEach(function (button, i) {
                button.setAttribute('aria-selected', i === index ? 'true' : 'false');
            });
        }

        prev.addEventListener('click', function () { show(index - 1); });
        next.addEventListener('click', function () { show(index + 1); });

        // Arrow keys, but only once the slider has been interacted with or scrolled to — binding them to the
        // document unconditionally would steal them from someone reading the rest of the page.
        slider.addEventListener('keydown', function (event) {
            if (event.key === 'ArrowLeft') { show(index - 1); event.preventDefault(); }
            if (event.key === 'ArrowRight') { show(index + 1); event.preventDefault(); }
        });

        // Swipe, for the platform most likely to be reading this on a phone.
        var startX = null;
        slider.addEventListener('touchstart', function (event) {
            startX = event.touches[0].clientX;
        }, { passive: true });

        slider.addEventListener('touchend', function (event) {
            if (startX === null) return;
            var dx = event.changedTouches[0].clientX - startX;
            if (Math.abs(dx) > 40) show(index + (dx < 0 ? 1 : -1));
            startX = null;
        }, { passive: true });

        slider.classList.add('live');
        slider.setAttribute('tabindex', '0');
        show(0);
    });
})();

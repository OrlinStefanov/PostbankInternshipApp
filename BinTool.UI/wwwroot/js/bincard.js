// Drag-to-turn for the BIN card.
//
// Plain JS on purpose. A pointermove fires dozens of times a second, and routing each one
// through a Blazor handler would put a server round trip between the hand and the card.
// Here the angles are written straight to CSS custom properties on the scene element, so a
// drag costs a repaint and nothing else, and stays smooth however busy the circuit is.
//
// The flip is left to Blazor: it happens once per click, it is part of what the component
// is showing, and it composes with the drag angle in the transform rather than fighting it.

// Keyed by the scene element rather than held as module state, because a page can show more
// than one card at a time - the editor's live preview sits on screen beside nothing else,
// but the lookup screen and a details modal could easily coincide. A WeakMap also lets a
// scene that is torn down without a detach fall out on its own.
const bound = new WeakMap();

/**
 * Binds pointer dragging to a card scene. Rebinding replaces any previous binding on the
 * same element, so a modal that opens a second time does not stack handlers.
 */
export function attach(scene) {
    if (!scene) return;

    detach(scene);

    let dragging = false;
    let pointerId = null;
    let startX = 0;
    let startY = 0;
    let baseY = 0;
    let baseX = 0;
    let ry = 0;
    let rx = 0;

    const clamp = (value, low, high) => Math.min(high, Math.max(low, value));

    const apply = () => {
        scene.style.setProperty('--drag-y', `${ry}deg`);
        scene.style.setProperty('--drag-x', `${rx}deg`);
    };

    const onDown = e => {
        // Only the primary button drags, so a right-click can still reach the context menu.
        if (e.button !== 0) return;

        dragging = true;
        pointerId = e.pointerId;
        startX = e.clientX;
        startY = e.clientY;
        baseY = ry;
        baseX = rx;
        scene.classList.add('is-dragging');

        try {
            scene.setPointerCapture(pointerId);
        } catch {
            // Capture only keeps the drag alive past the edge of the element; without it
            // the drag still works, it just ends early.
        }
    };

    const onMove = e => {
        if (!dragging) return;

        ry = baseY + (e.clientX - startX) * 0.45;

        // Tilt is clamped. Past roughly a third of a turn the card is edge-on and there is
        // nothing left to read, whereas yaw can wind as far as the hand wants.
        rx = clamp(baseX - (e.clientY - startY) * 0.45, -34, 34);

        apply();
        e.preventDefault();
    };

    const onUp = () => {
        if (!dragging) return;

        dragging = false;
        scene.classList.remove('is-dragging');

        if (pointerId !== null) {
            try {
                scene.releasePointerCapture(pointerId);
            } catch {
                // Already released - releasing twice is not an error worth reporting.
            }
            pointerId = null;
        }
    };

    scene.addEventListener('pointerdown', onDown);
    scene.addEventListener('pointermove', onMove);
    scene.addEventListener('pointerup', onUp);
    scene.addEventListener('pointercancel', onUp);
    scene.addEventListener('lostpointercapture', onUp);

    bound.set(scene, {
        reset() {
            ry = 0;
            rx = 0;
            apply();
        },
        teardown() {
            scene.removeEventListener('pointerdown', onDown);
            scene.removeEventListener('pointermove', onMove);
            scene.removeEventListener('pointerup', onUp);
            scene.removeEventListener('pointercancel', onUp);
            scene.removeEventListener('lostpointercapture', onUp);

            scene.style.removeProperty('--drag-y');
            scene.style.removeProperty('--drag-x');
            scene.classList.remove('is-dragging');
        }
    });
}

/** Returns the card to square on. The flip is Blazor's to undo. */
export function reset(scene) {
    bound.get(scene)?.reset();
}

export function detach(scene) {
    const state = bound.get(scene);
    if (!state) return;

    state.teardown();
    bound.delete(scene);
}

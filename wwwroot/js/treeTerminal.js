// Fullscreen Interactive Terminal Canvas & Audio Engine for Merry Christmas Tree
(function () {
    const charCache = new Array(65536);

    let canvas = null;
    let ctx = null;
    let dotNetRef = null;
    let audio = null;

    let isAudioPlaying = false;
    let isMuted = false;
    let isCrtEnabled = false;

    let cols = 100;
    let rows = 45;
    let cellW = 10;
    let cellH = 18;
    let dpr = 1;

    let lastTime = 0;
    let isRunning = false;
    let isPausedByVisibility = false;

    window.treeTerminal = {
        init: function (canvasElement, audioElement, dotNetReference) {
            canvas = canvasElement;
            ctx = canvas.getContext('2d', { alpha: false });
            audio = audioElement;
            dotNetRef = dotNetReference;

            this.resize();
            window.addEventListener('resize', () => this.resize());

            // Ensure container receives keyboard events immediately
            const container = document.getElementById('main-container');
            if (container) container.focus();

            // Mouse hover hit-testing inside the terminal
            canvas.addEventListener('mousemove', (e) => {
                if (!dotNetRef) return;
                const col = Math.floor(e.clientX / cellW);
                const row = Math.floor(e.clientY / cellH);
                try {
                    const isHovering = dotNetRef.invokeMethod('OnCanvasHover', col, row);
                    canvas.style.cursor = isHovering ? 'pointer' : 'default';
                } catch {
                    // Fallback if sync invoke unavailable
                }
            });

            canvas.addEventListener('mouseleave', () => {
                canvas.style.cursor = 'default';
                if (dotNetRef) {
                    try {
                        dotNetRef.invokeMethod('OnCanvasHover', -1, -1);
                    } catch { }
                }
            });

            // Click hit-testing inside the terminal
            canvas.addEventListener('click', (e) => {
                if (container) container.focus();

                if (!dotNetRef) {
                    if (!isAudioPlaying && !isMuted) {
                        this.playAudio();
                    }
                    return;
                }

                const col = Math.floor(e.clientX / cellW);
                const row = Math.floor(e.clientY / cellH);

                try {
                    const action = dotNetRef.invokeMethod('OnCanvasClick', col, row);
                    if (action === 'music') {
                        const muted = this.toggleMute();
                        dotNetRef.invokeMethod('SyncAudioMuted', muted);
                    } else {
                        if (!isAudioPlaying && !isMuted) {
                            this.playAudio();
                        }
                        if (action === 'crt') {
                            const crt = this.toggleCrt();
                            dotNetRef.invokeMethod('SyncCrtState', crt);
                        } else if (action === 'fullscreen') {
                            this.toggleFullscreen();
                        }
                    }
                } catch (err) {
                    console.error("Click handling error:", err);
                }
            });

            // Handle page visibility change (pause animation when tab hidden to save battery & CPU)
            document.addEventListener('visibilitychange', () => {
                if (document.hidden) {
                    isPausedByVisibility = true;
                } else {
                    isPausedByVisibility = false;
                    lastTime = performance.now();
                }
            });

            // Start animation loop
            isRunning = true;
            lastTime = performance.now();
            requestAnimationFrame((t) => this.loop(t));

            // Try immediate autoplay (succeeds if browser policy or prior interaction allows it)
            this.playAudio();

            // Setup audio on first gesture (keyboard/touch/click)
            const startAudioOnGesture = (e) => {
                // Let HandleKeyDown -> toggleMute handle 'M' so it doesn't race and mute on first press
                if (e && e.type === 'keydown' && e.key && e.key.toLowerCase() === 'm') {
                    return;
                }
                if (!isAudioPlaying && !isMuted) {
                    this.playAudio();
                }
                window.removeEventListener('click', startAudioOnGesture);
                window.removeEventListener('keydown', startAudioOnGesture);
                window.removeEventListener('touchstart', startAudioOnGesture);
            };
            window.addEventListener('click', startAudioOnGesture);
            window.addEventListener('keydown', startAudioOnGesture);
            window.addEventListener('touchstart', startAudioOnGesture);
        },

        resize: function () {
            if (!canvas) return;

            const windowW = window.innerWidth;
            const windowH = window.innerHeight;

            dpr = Math.min(window.devicePixelRatio || 1, 2);

            const targetCellH = Math.max(14, Math.min(22, Math.floor(windowH / 50)));
            rows = Math.max(25, Math.floor(windowH / targetCellH));

            const targetCellW = targetCellH * 0.53;
            cols = Math.max(50, Math.floor(windowW / targetCellW));

            cellW = windowW / cols;
            cellH = windowH / rows;

            canvas.width = Math.round(windowW * dpr);
            canvas.height = Math.round(windowH * dpr);

            canvas.style.width = '100vw';
            canvas.style.height = '100vh';
        },

        loop: function (timestamp) {
            if (!isRunning) return;

            requestAnimationFrame((t) => this.loop(t));

            if (isPausedByVisibility || !dotNetRef) {
                lastTime = timestamp;
                return;
            }

            const dt = lastTime === 0 ? 0.016 : Math.min((timestamp - lastTime) / 1000, 0.1);
            lastTime = timestamp;

            dotNetRef.invokeMethodAsync('OnFrame', dt, cols, rows).then((frameBytes) => {
                if (frameBytes) {
                    this.render(frameBytes);
                }
            }).catch(err => {
                console.error("Frame render error:", err);
            });
        },

        render: function (frameBytes) {
            if (!ctx || !canvas) return;

            const total = cols * rows;
            const scaledCellW = cellW * dpr;
            const scaledCellH = cellH * dpr;

            ctx.fillStyle = '#030509';
            ctx.fillRect(0, 0, canvas.width, canvas.height);

            const fontSize = Math.floor(scaledCellH * 0.88);
            ctx.font = `${fontSize}px "Cascadia Code", "Fira Code", Consolas, "Courier New", monospace`;
            ctx.textBaseline = 'middle';
            ctx.textAlign = 'center';

            let currentFgColor = -1;

            for (let i = 0; i < total; i++) {
                const offset = i * 8;
                if (offset + 7 >= frameBytes.length) break;

                const charCode = frameBytes[offset] | (frameBytes[offset + 1] << 8);

                const bgR = frameBytes[offset + 5];
                const bgG = frameBytes[offset + 6];
                const bgB = frameBytes[offset + 7];

                const col = i % cols;
                const row = Math.floor(i / cols);
                const x = col * scaledCellW;
                const y = row * scaledCellH;

                if (bgR !== 0 || bgG !== 0 || bgB !== 0) {
                    ctx.fillStyle = `rgb(${bgR},${bgG},${bgB})`;
                    ctx.fillRect(x, y, Math.ceil(scaledCellW), Math.ceil(scaledCellH));
                    currentFgColor = -1;
                }

                if (charCode > 32) {
                    const fgR = frameBytes[offset + 2];
                    const fgG = frameBytes[offset + 3];
                    const fgB = frameBytes[offset + 4];
                    const fgColorKey = (fgR << 16) | (fgG << 8) | fgB;

                    if (fgColorKey !== currentFgColor) {
                        ctx.fillStyle = `rgb(${fgR},${fgG},${fgB})`;
                        currentFgColor = fgColorKey;
                    }

                    let glyph = charCache[charCode];
                    if (!glyph) {
                        glyph = String.fromCharCode(charCode);
                        charCache[charCode] = glyph;
                    }

                    ctx.fillText(glyph, x + scaledCellW / 2, y + scaledCellH / 2);
                }
            }
        },

        playAudio: function () {
            if (!audio) return;
            audio.muted = isMuted;
            const playPromise = audio.play();
            if (playPromise !== undefined) {
                playPromise.then(() => {
                    isAudioPlaying = true;
                }).catch(e => {
                    isAudioPlaying = false;
                    console.log("Audio play postponed until gesture:", e);
                });
            }
        },

        pauseAudio: function () {
            if (audio) {
                audio.pause();
                isAudioPlaying = false;
            }
        },

        toggleMute: function () {
            if (!audio) return false;
            // If autoplay was blocked and audio hasn't started yet, start it unmuted on the first press
            if (audio.paused) {
                isMuted = false;
                audio.muted = false;
                this.playAudio();
                return false;
            }
            isMuted = !isMuted;
            audio.muted = isMuted;
            return isMuted;
        },

        toggleCrt: function () {
            isCrtEnabled = !isCrtEnabled;
            const container = document.getElementById('main-container');
            if (container) {
                container.classList.toggle('crt-active', isCrtEnabled);
            }
            return isCrtEnabled;
        },

        toggleFullscreen: function () {
            if (!document.fullscreenElement) {
                document.documentElement.requestFullscreen().catch(() => {});
            } else {
                if (document.exitFullscreen) {
                    document.exitFullscreen().catch(() => {});
                }
            }
        }
    };
})();

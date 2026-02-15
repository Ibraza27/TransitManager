// Public Homepage JS - Particles, Scroll, Counters
window.initPublicHomepage = function () {
    // Particles
    var canvas = document.getElementById('particles-canvas');
    if (!canvas) return;
    var ctx = canvas.getContext('2d');
    var particles = [];
    var mouseX = 0, mouseY = 0;
    function resizeCanvas() { canvas.width = window.innerWidth; canvas.height = window.innerHeight; }
    resizeCanvas();
    window.addEventListener('resize', resizeCanvas);
    document.addEventListener('mousemove', function (e) { mouseX = e.clientX; mouseY = e.clientY; });

    var count = Math.min(80, Math.floor(window.innerWidth / 15));
    for (var i = 0; i < count; i++) {
        particles.push({
            x: Math.random() * canvas.width, y: Math.random() * canvas.height,
            size: Math.random() * 2 + 0.5, speedX: (Math.random() - 0.5) * 0.5, speedY: (Math.random() - 0.5) * 0.5,
            opacity: Math.random() * 0.5 + 0.1, color: Math.random() > 0.7 ? '#E8752A' : '#F5A623'
        });
    }

    function animate() {
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        for (var i = 0; i < particles.length; i++) {
            var p = particles[i];
            p.x += p.speedX; p.y += p.speedY;
            var dx = p.x - mouseX, dy = p.y - mouseY, dist = Math.sqrt(dx * dx + dy * dy);
            if (dist < 120) { p.x += (dx / dist) * 1.5; p.y += (dy / dist) * 1.5; }
            if (p.x < 0 || p.x > canvas.width) p.speedX *= -1;
            if (p.y < 0 || p.y > canvas.height) p.speedY *= -1;
            ctx.beginPath(); ctx.arc(p.x, p.y, p.size, 0, Math.PI * 2);
            ctx.fillStyle = p.color; ctx.globalAlpha = p.opacity; ctx.fill(); ctx.globalAlpha = 1;
            for (var j = i + 1; j < particles.length; j++) {
                var q = particles[j];
                var d = Math.sqrt((p.x - q.x) * (p.x - q.x) + (p.y - q.y) * (p.y - q.y));
                if (d < 150) {
                    ctx.beginPath(); ctx.moveTo(p.x, p.y); ctx.lineTo(q.x, q.y);
                    ctx.strokeStyle = 'rgba(232,117,42,' + (0.08 * (1 - d / 150)) + ')';
                    ctx.lineWidth = 0.5; ctx.stroke();
                }
            }
        }
        requestAnimationFrame(animate);
    }
    animate();

    // Force video autoplay on iOS / mobile
    var heroVideo = document.querySelector('.ph-hero-video-bg video');
    if (heroVideo) {
        heroVideo.play().catch(function () {
            // Autoplay blocked — show poster as background
            var bg = document.querySelector('.ph-hero-video-bg');
            if (bg) {
                bg.style.backgroundImage = 'url(' + heroVideo.getAttribute('poster') + ')';
                bg.style.backgroundSize = 'cover';
                bg.style.backgroundPosition = 'center';
            }
        });
        // Also try on first user interaction
        var tryPlay = function () {
            heroVideo.play().catch(function () { });
            document.removeEventListener('touchstart', tryPlay);
            document.removeEventListener('click', tryPlay);
        };
        document.addEventListener('touchstart', tryPlay, { once: true });
        document.addEventListener('click', tryPlay, { once: true });
    }

    // Nav scroll
    window.addEventListener('scroll', function () {
        var nav = document.getElementById('ph-navbar');
        if (nav) { if (window.scrollY > 50) nav.classList.add('scrolled'); else nav.classList.remove('scrolled'); }
    });

    // Burger
    var burger = document.getElementById('phBurger');
    if (burger) {
        burger.addEventListener('click', function () {
            document.getElementById('phNavLinks').classList.toggle('open');
        });
    }
    document.querySelectorAll('.ph-nav-links a').forEach(function (a) {
        a.addEventListener('click', function () { document.getElementById('phNavLinks').classList.remove('open'); });
    });

    // Scroll reveal
    var observer = new IntersectionObserver(function (entries) {
        entries.forEach(function (entry) { if (entry.isIntersecting) entry.target.classList.add('visible'); });
    }, { threshold: 0.1, rootMargin: '0px 0px -50px 0px' });
    document.querySelectorAll('.ph-reveal').forEach(function (el) { observer.observe(el); });

    // Counter animation
    var counterObs = new IntersectionObserver(function (entries) {
        entries.forEach(function (entry) {
            if (entry.isIntersecting) {
                var el = entry.target;
                var target = parseInt(el.dataset.target);
                var current = 0, increment = target / 60;
                var timer = setInterval(function () {
                    current += increment;
                    if (current >= target) { current = target; clearInterval(timer); }
                    el.textContent = Math.floor(current) + '+';
                }, 30);
                counterObs.unobserve(el);
            }
        });
    }, { threshold: 0.5 });
    document.querySelectorAll('.ph-number').forEach(function (el) { counterObs.observe(el); });

    // Smooth scroll
    document.querySelectorAll('.public-home a[href^="#"]').forEach(function (anchor) {
        anchor.addEventListener('click', function (e) {
            e.preventDefault();
            var t = document.querySelector(this.getAttribute('href'));
            if (t) t.scrollIntoView({ behavior: 'smooth', block: 'start' });
        });
    });
};

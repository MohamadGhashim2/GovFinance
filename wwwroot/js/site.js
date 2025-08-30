// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
document.addEventListener("click", (e) => {
    const el = e.target.closest("[data-confirm]");
    if (el && !confirm(el.getAttribute("data-confirm"))) e.preventDefault();
});
document.addEventListener('DOMContentLoaded', function () {
    var btn = document.getElementById('themeToggle');
    if (!btn) return;

    function currTheme() {
        return document.documentElement.getAttribute('data-bs-theme') === 'dark' ? 'dark' : 'light';
    }
    function setTheme(t) {
        document.documentElement.setAttribute('data-bs-theme', t);
        try { localStorage.setItem('theme', t); } catch (e) { }
        btn.textContent = (t === 'dark') ? '☀️' : '🌙';
        btn.setAttribute('aria-label', t === 'dark' ? 'تبديل إلى الوضع الفاتح' : 'تبديل إلى الوضع الليلي');
    }

    // ضبط الأيقونة عند التحميل
    setTheme(currTheme());

    btn.addEventListener('click', function () {
        setTheme(currTheme() === 'dark' ? 'light' : 'dark');
    });
});

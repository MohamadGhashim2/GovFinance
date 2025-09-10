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
// == Quick Amount Pad ==
// يقرأ أي كونتينر فيه data-amount-pad ويعدّل الحقل المحدد في data-target
(function () {
    function normalizeDigits(str) {
        if (str == null) return '';
        str = String(str)
            .replace(/[\u0660-\u0669]/g, d => '0123456789'[d.charCodeAt(0) - 0x0660]) // أرقام عربية
            .replace(/[\u06F0-\u06F9]/g, d => '0123456789'[d.charCodeAt(0) - 0x06F0]) // أرقام فارسية
            .replace(/[،٬]/g, '')  // فواصل آلاف عربية
            .replace(/[٫]/g, '.'); // فاصلة عشرية عربية
        return str;
    }
    function toNum(v) {
        const s = normalizeDigits(v).replace(/[^\d.\-]/g, '');
        const n = parseFloat(s);
        return isFinite(n) ? n : 0;
    }
    function format(n) {
        return Number.isInteger(n) ? String(n) : n.toFixed(2);
    }

    document.addEventListener('click', function (e) {
        const btn = e.target.closest('[data-inc],[data-set]');
        if (!btn) return;
        const pad = btn.closest('[data-amount-pad]');
        if (!pad) return;

        const targetSel = pad.getAttribute('data-target');
        const input = document.querySelector(targetSel);
        if (!input || input.readOnly || input.disabled) return;

        let val = toNum(input.value);
        if (btn.hasAttribute('data-set')) {
            val = toNum(btn.getAttribute('data-set'));
        } else {
            val += toNum(btn.getAttribute('data-inc'));
        }

        // حدود اختيارية
        const min = pad.hasAttribute('data-min') ? toNum(pad.getAttribute('data-min')) : null;
        const max = pad.hasAttribute('data-max') ? toNum(pad.getAttribute('data-max')) : null;
        if (min != null && val < min) val = min;
        if (max != null && val > max) val = max;

        input.value = format(val);

        // حفّز أي listeners (مثل إعادة حساب المقبوض/الباقي)
        input.dispatchEvent(new Event('input', { bubbles: true }));
        input.dispatchEvent(new Event('change', { bubbles: true }));
    });
})();

/* =========================================================================
   MatchdayApi — helper dùng chung cho toàn bộ trang frontend tĩnh.
   Gọi thẳng vào Web API thật (không giả lập dữ liệu). Vì các trang HTML này
   được ASP.NET Core phục vụ tĩnh từ wwwroot cùng domain với API, apiBase()
   mặc định lấy window.location.origin — chạy đúng cả lúc dev (localhost)
   lẫn sau khi deploy (task #26) mà không cần sửa code.
   ========================================================================= */

const MDA = (() => {
    const TOKEN_KEY = 'mda_token';
    const USER_KEY = 'mda_user';

    function apiBase() {
        return window.location.origin;
    }

    function getToken() {
        return localStorage.getItem(TOKEN_KEY);
    }

    function getUser() {
        const raw = localStorage.getItem(USER_KEY);
        return raw ? JSON.parse(raw) : null;
    }

    function isLoggedIn() {
        return !!getToken();
    }

    function saveSession(authResponse) {
        localStorage.setItem(TOKEN_KEY, authResponse.token);
        localStorage.setItem(USER_KEY, JSON.stringify({
            userId: authResponse.userId,
            fullName: authResponse.fullName,
            email: authResponse.email,
            role: authResponse.role
        }));
    }

    function logout() {
        localStorage.removeItem(TOKEN_KEY);
        localStorage.removeItem(USER_KEY);
        window.location.href = 'index.html';
    }

    function requireLogin() {
        if (!isLoggedIn()) {
            window.location.href = 'login.html?next=' + encodeURIComponent(window.location.pathname + window.location.search);
            return false;
        }
        return true;
    }

    /** Chặn truy cập các trang admin-*.html nếu chưa đăng nhập hoặc không phải Admin. */
    function requireAdmin() {
        if (!requireLogin()) return false;
        const user = getUser();
        if (!user || user.role !== 'Admin') {
            toast('Bạn không có quyền truy cập trang quản trị.', 'error');
            window.location.href = 'index.html';
            return false;
        }
        return true;
    }

    /**
     * Wrapper fetch: tự thêm Authorization header (nếu có token), tự parse JSON,
     * và ném lỗi có message tiếng Việt lấy từ body API khi request thất bại.
     */
    async function api(path, options = {}) {
        const headers = Object.assign({}, options.headers || {});
        const token = getToken();
        if (token) headers['Authorization'] = `Bearer ${token}`;
        if (options.body && !(options.body instanceof FormData)) {
            headers['Content-Type'] = 'application/json';
        }

        let res;
        try {
            res = await fetch(apiBase() + path, Object.assign({}, options, { headers }));
        } catch (err) {
            throw new Error('Không kết nối được máy chủ. Kiểm tra lại kết nối mạng.');
        }

        if (res.status === 204) return null;

        const contentType = res.headers.get('content-type') || '';
        const isJson = contentType.includes('application/json');
        const data = isJson ? await res.json().catch(() => null) : await res.text();

        if (!res.ok) {
            const message = (isJson && data && data.message) ? data.message : `Lỗi ${res.status}`;
            if (res.status === 401) {
                // Token hết hạn/không hợp lệ — dọn phiên và điều hướng lại trang đăng nhập.
                localStorage.removeItem(TOKEN_KEY);
                localStorage.removeItem(USER_KEY);
            }
            const err = new Error(message);
            err.status = res.status;
            throw err;
        }

        return data;
    }

    function money(n) {
        if (n === null || n === undefined) return '—';
        return Number(n).toLocaleString('vi-VN') + 'đ';
    }

    function formatDateTime(iso) {
        const d = new Date(iso);
        const day = d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' });
        const time = d.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
        return `${time} · ${day}`;
    }

    function timeOnly(iso) {
        return new Date(iso).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
    }

    function initials(name) {
        return (name || '').split(' ').filter(Boolean).slice(-2).map(w => w[0]).join('').toUpperCase();
    }

    /**
     * Huy hiệu đội bóng: trước đây mỗi đội được gán 1 trong 4 màu (vàng/xanh/đỏ/xám)
     * theo hash tên đội — nhìn rối vì lẫn nhiều tông ngoài bảng màu chính của site.
     * Giờ đồng bộ về đúng 1 kiểu: nền tối + chữ vàng, khớp toàn bộ giao diện còn lại.
     */
    function crestTheme() {
        return { bg: 'var(--bg-elevated)', fg: 'var(--gold-bright)' };
    }

    /**
     * Icon SVG vẽ tay (nét mảnh, dùng currentColor) — thay cho emoji hệ điều
     * hành (📍🗓🔍), vốn không đồng bộ nét vẽ với phần còn lại của giao diện.
     */
    const ICONS = {
        pin: '<svg width="13" height="13" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.4" style="flex-shrink:0;"><path d="M8 14.3S13 9.7 13 6.1A5 5 0 0 0 3 6.1C3 9.7 8 14.3 8 14.3Z"/><circle cx="8" cy="6.1" r="1.7"/></svg>',
        calendar: '<svg width="13" height="13" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.4" style="flex-shrink:0;"><rect x="2" y="3.4" width="12" height="10.6" rx="1.4"/><path d="M2 6.4h12M5 2v2.6M11 2v2.6"/></svg>',
        search: '<svg width="15" height="15" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5"><circle cx="7" cy="7" r="4.4"/><path d="M13.4 13.4 10.5 10.5" stroke-linecap="round"/></svg>',
        ball: '<svg width="13" height="13" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.2" stroke-linecap="round" stroke-linejoin="round" style="flex-shrink:0;"><circle cx="8" cy="8" r="6.2"/><path d="M8 8 4 5.4M8 8l4-2.6M8 8v5M8 8 3.7 10.4M8 8l4.3 2.4"/></svg>',
        assist: '<svg width="13" height="13" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.4" stroke-linecap="round" stroke-linejoin="round" style="flex-shrink:0;"><path d="M2.2 8h11M9.2 4.2l4 3.8-4 3.8"/></svg>',
        lock: '<svg width="26" height="26" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.2" stroke-linecap="round" stroke-linejoin="round"><rect x="3.3" y="7" width="9.4" height="6.5" rx="1.3"/><path d="M5.2 7V4.6a2.8 2.8 0 0 1 5.6 0V7"/></svg>',
        warning: '<svg width="26" height="26" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.2" stroke-linecap="round" stroke-linejoin="round"><path d="M8 1.6 14.8 13.4a1 1 0 0 1-.9 1.5H2.1a1 1 0 0 1-.9-1.5L8 1.6Z"/><path d="M8 6.2v3.4M8 11.6h.01"/></svg>',
        ticket: '<svg width="30" height="30" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.2" stroke-linecap="round" stroke-linejoin="round"><path d="M3 8a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2v1.4a1.7 1.7 0 0 0 0 3.2V14a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-1.4a1.7 1.7 0 0 0 0-3.2V8Z"/><path d="M14 6v12" stroke-dasharray="1.6 1.6"/></svg>',
        food: '<svg width="16" height="16" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.2" stroke-linecap="round" stroke-linejoin="round"><path d="M4 1.5v4.2M5.2 1.5v4.2M6.4 1.5v4.2M5.2 5.7V14.5"/><path d="M11 1.5c-1.4 0-2.2 1.6-2.2 3.4S9.6 8.3 11 8.3V14.5"/></svg>',
    };
    function icon(name) {
        return ICONS[name] || '';
    }

    function toast(message, type = 'default') {
        let root = document.getElementById('toast-root');
        if (!root) {
            root = document.createElement('div');
            root.id = 'toast-root';
            document.body.appendChild(root);
        }
        const el = document.createElement('div');
        el.className = 'toast' + (type !== 'default' ? ' ' + type : '');
        el.textContent = message;
        root.appendChild(el);
        setTimeout(() => {
            el.style.opacity = '0';
            el.style.transition = 'opacity .25s';
            setTimeout(() => el.remove(), 260);
        }, 3600);
    }

    function statusBadge(status) {
        const map = {
            Upcoming: ['badge-upcoming', 'Sắp diễn ra'],
            Live: ['badge-live', 'Đang diễn ra'],
            Finished: ['badge-finished', 'Đã kết thúc'],
            Pending: ['badge-pending', 'Chờ thanh toán'],
            Paid: ['badge-paid', 'Đã thanh toán'],
            Cancelled: ['badge-cancelled', 'Đã hủy'],
            Refunded: ['badge-refunded', 'Đã hoàn tiền'],
            Rejected: ['badge-rejected', 'Bị từ chối'],
            Approved: ['badge-paid', 'Đã duyệt']
        };
        const [cls, label] = map[status] || ['badge-finished', status];
        const dot = status === 'Live' ? '<span class="pulse-dot"></span>' : '';
        return `<span class="badge ${cls}">${dot}${label}</span>`;
    }

    /** Render header dùng chung — gọi renderHeader('nav-id-active') ở mỗi trang. */
    function renderHeader(activeId) {
        const mount = document.getElementById('site-header');
        if (!mount) return;
        const user = getUser();

        const links = [
            { id: 'home', href: 'index.html', label: 'Trang chủ' },
        ];
        // "Đồ ăn & thức uống" (food-menu.html) là trang khách xem/đặt món — không phải
        // trang quản lý. Admin quản lý món ăn ở admin-food.html (link "Đồ ăn" trong khu
        // Quản trị) nên ẩn link khách này đi, tránh nhầm lẫn 2 trang.
        // Tương tự "Vé của tôi" cũng không phải luồng của Admin.
        if (!user || user.role !== 'Admin') {
            links.push({ id: 'food', href: 'food-menu.html', label: 'Đồ ăn & thức uống' });
        }
        if (user && user.role !== 'Admin') links.push({ id: 'tickets', href: 'my-tickets.html', label: 'Vé của tôi' });

        const navHtml = links.map(l =>
            `<a class="nav-link${l.id === activeId ? ' active' : ''}" href="${l.href}">${l.label}</a>`
        ).join('');

        const adminLinkHtml = (user && user.role === 'Admin')
            ? `<a class="nav-link${activeId === 'admin' ? ' active' : ''}" href="admin-dashboard.html">Quản trị</a>`
            : '';

        // Bấm vào tên tài khoản (cả User lẫn Admin) vào trang hồ sơ "Bio" — xem thông tin
        // tài khoản. "Vé của tôi" / "Quản trị" đã có link riêng trên nav nên không trùng lặp.
        const accountHref = 'account.html';

        const rightHtml = user
            ? `${adminLinkHtml}
         <a class="nav-link${activeId === 'account' ? ' active' : ''}" href="${accountHref}">${user.fullName}</a>
         <button class="btn btn-ghost btn-sm" id="btn-logout">Đăng xuất</button>`
            : `<a class="btn btn-gold btn-sm" href="login.html">Đăng nhập</a>`;

        mount.innerHTML = `
      <header class="site-header">
        <div class="container bar">
          <a class="brand" href="index.html">MATCHDAY<span class="dot">.</span><small>SÂN VẬN ĐỘNG</small></a>
          <nav class="nav-links">${navHtml}${rightHtml}</nav>
        </div>
      </header>`;

        const btn = document.getElementById('btn-logout');
        if (btn) btn.addEventListener('click', logout);
    }

    function renderFooter() {
        const mount = document.getElementById('site-footer');
        if (!mount) return;
        mount.innerHTML = `<footer class="site-footer container">MatchdayApi — dự án cá nhân phục vụ học tập / portfolio. Tạ Tuấn Phát · HUTECH.</footer>`;
    }

    function qs(name) {
        return new URLSearchParams(window.location.search).get(name);
    }

    /** Thanh tab phụ dùng chung cho mọi trang admin-*.html — gọi renderAdminNav('tab-id'). */
    function renderAdminNav(activeId) {
        const mount = document.getElementById('admin-nav');
        if (!mount) return;
        const tabs = [
            { id: 'dashboard', href: 'admin-dashboard.html', label: 'Tổng quan' },
            { id: 'matches', href: 'admin-matches.html', label: 'Trận đấu' },
            { id: 'food', href: 'admin-food.html', label: 'Đồ ăn' },
            { id: 'refunds', href: 'admin-refunds.html', label: 'Hoàn tiền' },
            { id: 'catalog', href: 'admin-catalog.html', label: 'Dữ liệu nền' },
        ];
        mount.innerHTML = tabs.map(t =>
            `<a class="admin-tab${t.id === activeId ? ' active' : ''}" href="${t.href}">${t.label}</a>`
        ).join('');
    }

    return {
        apiBase, getToken, getUser, isLoggedIn, saveSession, logout, requireLogin, requireAdmin,
        api, money, formatDateTime, timeOnly, initials, crestTheme, icon, toast, statusBadge,
        renderHeader, renderFooter, renderAdminNav, qs
    };
})();
// MealShop client script - ho tro tuong tac khong reload trang va thong bao Toast popup

document.addEventListener('DOMContentLoaded', function () {
    // 1. Khoi tao Toast tu Server (TempData) neu co, tu dong bien mat sau 3 giay
    var flashToastEl = document.getElementById('flashToast');
    if (flashToastEl && typeof bootstrap !== 'undefined') {
        var flashToast = new bootstrap.Toast(flashToastEl, { delay: 3000, autohide: true });
        flashToast.show();
    }

    // 2. Ham hien thi thong bao popup Toast goc tren phai
    window.showToast = function (message, type) {
        type = type || 'success';
        var toastEl = document.getElementById('dynamicToast');
        var msgEl = document.getElementById('toastMessage');
        var iconEl = document.getElementById('toastIcon');

        if (!toastEl || !msgEl || typeof bootstrap === 'undefined') {
            return;
        }

        msgEl.textContent = message;

        if (type === 'warning') {
            toastEl.style.borderLeft = '5px solid #E28743';
            if (iconEl) { iconEl.textContent = '⚠'; iconEl.style.color = '#E28743'; }
        } else if (type === 'danger') {
            toastEl.style.borderLeft = '5px solid #DC3545';
            if (iconEl) { iconEl.textContent = '✕'; iconEl.style.color = '#DC3545'; }
        } else {
            toastEl.style.borderLeft = '5px solid #2F7D4F';
            if (iconEl) { iconEl.textContent = '✓'; iconEl.style.color = '#2F7D4F'; }
        }

        var toast = new bootstrap.Toast(toastEl, { delay: 3000, autohide: true });
        toast.show();
    };

    // 3. Ham cap nhat rieng bang MiniCart ben phai ma khong can tai lai trang
    window.reloadMiniCart = function () {
        var container = document.getElementById('miniCartContainer');
        if (!container) return;

        fetch('/Cart/MiniCart', {
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        })
        .then(function (res) {
            return res.text();
        })
        .then(function (html) {
            container.innerHTML = html;
        })
        .catch(function (err) {
            console.error('Loi cap nhat MiniCart:', err);
        });
    };

    // 4. Chan reload trang khi submit form gio hang (Them mon, Xoa mon, Sua so luong)
    document.addEventListener('submit', function (e) {
        var form = e.target;
        if (!form || form.tagName !== 'FORM') return;

        var action = form.getAttribute('action') || '';
        var isCartAction = action.includes('/Cart/Add')
            || action.includes('/Cart/Update')
            || action.includes('/Cart/Remove')
            || action.includes('/Cart/Clear');

        if (!isCartAction) return;

        // Chan reload mac dinh cua trinh duyet
        e.preventDefault();

        var formData = new FormData(form);

        fetch(action, {
            method: 'POST',
            body: formData,
            headers: {
                'X-Requested-With': 'XMLHttpRequest',
                'Accept': 'application/json'
            }
        })
        .then(function (response) {
            if (!response.headers.get('content-type')?.includes('application/json')) {
                throw new Error('Phản hồi không hợp lệ');
            }
            return response.json();
        })
        .then(function (data) {
            if (data && data.message) {
                window.showToast(data.message, data.type || 'success');
            }

            // Cap nhat MiniCart ben phai
            window.reloadMiniCart();

            // Neu dang o trang gio hang chi tiet /Cart/Index, tai lai trang sau nhip ngan
            var path = window.location.pathname.toLowerCase();
            if (path.endsWith('/cart') || path.endsWith('/cart/index')) {
                setTimeout(function () {
                    window.location.reload();
                }, 300);
            }
        })
        .catch(function (err) {
            console.warn('Không thể xác nhận thao tác giỏ hàng:', err);
            window.showToast('Chưa thể xác nhận thay đổi. Vui lòng kiểm tra giỏ hàng trước khi thử lại.', 'warning');
            window.reloadMiniCart();
        });
    });
});

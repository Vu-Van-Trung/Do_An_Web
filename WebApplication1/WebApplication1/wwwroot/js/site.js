// Hamburger menu toggle
document.addEventListener('DOMContentLoaded', () => {
    const toggle = document.getElementById('nav-toggle');
    const links  = document.getElementById('nav-links');
    if (toggle && links) {
        toggle.addEventListener('click', () => {
            const open = links.classList.toggle('nav-open');
            toggle.setAttribute('aria-expanded', open);
            toggle.textContent = open ? '\u2715' : '\u2630';
        });

        // Đóng menu khi click bên ngoài
        document.addEventListener('click', (e) => {
            if (!toggle.contains(e.target) && !links.contains(e.target)) {
                links.classList.remove('nav-open');
                toggle.setAttribute('aria-expanded', false);
                toggle.textContent = '\u2630';
            }
        });

        // Đóng menu khi chọn link
        links.querySelectorAll('a').forEach(a => {
            a.addEventListener('click', () => {
                links.classList.remove('nav-open');
                toggle.setAttribute('aria-expanded', false);
                toggle.textContent = '\u2630';
            });
        });
    }

    // User dropdown toggle
    const dropBtn  = document.getElementById('userDropdownBtn');
    const dropWrap = document.getElementById('userDropdown');
    if (dropBtn && dropWrap) {
        dropBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            const isOpen = dropWrap.classList.toggle('open');
            dropBtn.setAttribute('aria-expanded', isOpen);
        });

        // Đóng dropdown khi click bên ngoài
        document.addEventListener('click', (e) => {
            if (!dropWrap.contains(e.target)) {
                dropWrap.classList.remove('open');
                dropBtn.setAttribute('aria-expanded', false);
            }
        });
    }
});


async function refreshCartBadge() {
    const badge = document.getElementById('cart-badge');
    if (!badge) return;
    try {
        const res = await fetch('/Cart/Count');
        const data = await res.json();
        badge.textContent = data.count ?? 0;
    } catch {
        badge.textContent = '0';
    }
}

document.addEventListener('DOMContentLoaded', refreshCartBadge);

document.querySelectorAll('[data-add-cart]').forEach(btn => {
    btn.addEventListener('click', async (e) => {
        e.preventDefault();
        const productId = btn.dataset.productId;
        const res = await fetch('/Cart/Add', {
            method: 'POST',
            headers: {
                'X-Requested-With': 'XMLHttpRequest',
                'Content-Type': 'application/x-www-form-urlencoded'
            },
            body: `productId=${productId}&quantity=1`
        });
        const data = await res.json();
        if (data.success) {
            const badge = document.getElementById('cart-badge');
            if (badge) badge.textContent = data.count;
            btn.textContent = 'Added!';
            setTimeout(() => { btn.textContent = 'Add to Cart'; }, 1200);
        }
    });
});

document.querySelectorAll('[data-cart-qty]').forEach(input => {
    input.addEventListener('change', async () => {
        const cartItemId = input.dataset.cartItemId;
        const res = await fetch('/Cart/Update', {
            method: 'POST',
            headers: {
                'X-Requested-With': 'XMLHttpRequest',
                'Content-Type': 'application/x-www-form-urlencoded'
            },
            body: `cartItemId=${cartItemId}&quantity=${input.value}`
        });
        const data = await res.json();
        if (data.success) {
            document.querySelectorAll('[data-cart-subtotal]').forEach(el => {
                el.textContent = new Intl.NumberFormat('vi-VN').format(data.subtotal) + ' ₫';
            });
            const badge = document.getElementById('cart-badge');
            if (badge) badge.textContent = data.count;
        }
    });
});

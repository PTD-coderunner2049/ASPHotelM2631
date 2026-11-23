(() => {
    const searchForm = document.querySelector('[data-booking-search]');
    if (!searchForm) return;

    const checkIn = searchForm.elements.checkIn;
    const checkOut = searchForm.elements.checkOut;
    const resultTitle = document.querySelector('[data-booking-result-title]');
    const dialog = document.querySelector('[data-booking-dialog]');
    const notice = document.querySelector('[data-booking-notice]');
    const selectedRoom = document.querySelector('[data-selected-room]');
    const selectedPrice = document.querySelector('[data-selected-price]');

    const formatDate = (date) => new Intl.DateTimeFormat('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' }).format(date);
    const addDays = (date, days) => { const value = new Date(date); value.setDate(value.getDate() + days); return value; };
    const toInputDate = (date) => date.toISOString().slice(0, 10);

    const today = new Date();
    today.setHours(0, 0, 0, 0);
    checkIn.min = toInputDate(today);
    checkOut.min = toInputDate(addDays(today, 1));
    checkIn.value = toInputDate(addDays(today, 7));
    checkOut.value = toInputDate(addDays(today, 9));

    checkIn.addEventListener('change', () => {
        const minimumCheckout = addDays(new Date(checkIn.value), 1);
        checkOut.min = toInputDate(minimumCheckout);
        if (!checkOut.value || new Date(checkOut.value) <= new Date(checkIn.value)) checkOut.value = toInputDate(minimumCheckout);
    });

    searchForm.addEventListener('submit', (event) => {
        event.preventDefault();
        const from = new Date(checkIn.value);
        const to = new Date(checkOut.value);
        const nights = Math.max(1, Math.round((to - from) / 86400000));
        resultTitle.textContent = `${nights} đêm · ${formatDate(from)} – ${formatDate(to)}`;
        notice.hidden = true;
        document.querySelector('[data-booking-results]').scrollIntoView({ behavior: 'smooth', block: 'start' });
    });

    document.querySelectorAll('[data-book-room]').forEach((button) => {
        button.addEventListener('click', () => {
            selectedRoom.textContent = button.dataset.roomName;
            selectedPrice.textContent = `${button.dataset.roomPrice} / đêm`;
            dialog.showModal();
        });
    });

    document.querySelectorAll('[data-booking-close]').forEach((button) => button.addEventListener('click', () => dialog.close()));

    dialog.addEventListener('click', (event) => { if (event.target === dialog) dialog.close(); });

    document.querySelector('[data-booking-form]').addEventListener('submit', (event) => {
        event.preventDefault();
        dialog.close();
        notice.textContent = 'Yêu cầu đặt phòng demo đã được ghi nhận. Khi kết nối API, hệ thống sẽ tạo đơn và gửi xác nhận cho bạn.';
        notice.hidden = false;
        notice.scrollIntoView({ behavior: 'smooth', block: 'center' });
        event.currentTarget.reset();
    });
})();

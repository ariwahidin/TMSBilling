



function showToast(message, type = 'error') {
    const toast = document.createElement('div');
    toast.className = 'toast-alert';

    let icon = '❌'; // default error
    if (type === 'success') {
        toast.classList.add('success');
        icon = '✅';
    } else if (type === 'info') {
        toast.classList.add('info');
        icon = 'ℹ️';
    } else {
        toast.classList.add('error');
    }

    toast.innerHTML = `<span style="margin-right: 8px;">${icon}</span>${message}`;

    document.body.appendChild(toast);

    setTimeout(() => {
        toast.remove();
    }, 3000);
}


// global.js
$(function () {
    // handler global buat AJAX error
    $(document).ajaxError(function (event, jqxhr) {
        const res = jqxhr.responseJSON;
        if (res && res.errors) {
            showToast(res.errors.join(', '), 'error');
        } else if (res && res.message) {
            showToast(res.message, 'error');
        } else {
            showToast('An error occurred. Please try again.', 'error');
        }
    });


});




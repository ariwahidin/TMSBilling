



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
            showToast('Terjadi kesalahan. Silakan coba lagi.', 'error');
        }
    });


});




//$(function () {
//    $(document).ajaxError(function (event, jqxhr) {
//        const res = jqxhr.responseJSON;
//        if (res && res.errors) {
//            showToast(res.errors.join(', '), 'error');
//        } else if (res && res.message) {
//            showToast(res.message, 'error');
//        } else {
//            showToast('Terjadi kesalahan. Silakan coba lagi.', 'error');
//        }
//    });

//    // global.js
//    $(document).ready(function () {
//        // semua elemen select
//        $('select').select2({
//            placeholder: "Pilih data",
//            allowClear: true,
//            width: '100%'   // biar full lebar
//        });
//    });

//});



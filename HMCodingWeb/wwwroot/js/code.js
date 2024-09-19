$('#forgotPasswordLink').click(function() {
    $('#loginContainer').hide();
    $('#resetPasswordContainer').css({
        display: 'block',
        opacity: 0,
        // transform: 'translateX(100%)'
    }).animate({
        opacity: 1,
        // transform: 'translateX(0%)'
    }, 500); // Thực hiện animation trong 500 ms
});

$('#backToLogin').click(function() {
    var loginContainer = $('#loginContainer');
    var resetContainer = $('#resetPasswordContainer');

    // Hiển thị login container với hiệu ứng
    loginContainer.css({
        display: 'block',
        opacity: 0,
        // transform: 'translateX(-100%)'
    }).animate({
        opacity: 1,
        // transform: 'translateX(0)'
    }, 15, function() {
        resetContainer.hide(); // Ẩn reset password container sau khi hiệu ứng hoàn thành
    });
});
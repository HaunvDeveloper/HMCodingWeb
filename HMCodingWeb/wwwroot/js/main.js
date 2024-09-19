function PostDataWithObject(_type = "", _formData, _url = "", _messSuccess = "", _messError = "", idContainer = "", ReturnUrl = "") {
    $(document).ready(function () {
        if (_type === "add") {
            $.ajax({
                type: 'POST',
                url: _url,
                data: _formData,
                success: function (result) {
                    if (result.status) {
                        showToast("success", _messSuccess, 2500);
                        if (ReturnUrl != "") {
                            setTimeout(function () {
                                window.location.href = ReturnUrl;
                            }, 2000)
                        }
                        else if (result.url != "") {
                            setTimeout(function () {
                                window.location.href = result.url;
                            }, 2000)
                        }
                    }
                    else {
                        showToast("error", _messError, 2500);
                    }
                }
            });
        }
        else if (_type === "delete") {
            //var formData = $(_formData).val();
            $.ajax({
                type: 'POST',
                url: _url,
                data: { id: _formData },
                success: function (result) {
                    if (result.status) {
                        showToast("success", _messSuccess, 2500);
                        setTimeout(function () {
                            location.reload();
                        }, 2500)

                    }
                   
                    else {
                        showToast("error", _messError, 2500);
                    }
                }
            });

        }
        else if (_type === "edit") {
            $.ajax({
                type: 'POST',
                url: _url,
                data: _formData,
                success: function (result) {
                    if (result.status) {
                        showToast("success", _messSuccess, 2500);
                        if (ReturnUrl != "") {
                            setTimeout(function () {
                                window.location.href = ReturnUrl;
                            }, 2000)
                        }
                        
                    }

                    else {
                        showToast("error", _messError, 2500);
                    }
                }
            });
        }
        else if (_type === "search") {
            $.ajax({
                type: 'GET',
                url: _url,
                data: _formData,
                success: function (result) {
                    if (idContainer !== "") {

                        $(idContainer).html(result);
                    }
                }
            });
        }
    })
}
//Hàm hiển thị thông báo
function showToast(_type = "", _message = "Không có thông báo", _timer = 1500, _position = "top") {
    var Toast = Swal.mixin({
        toast: true,
        position: _position,
        showConfirmButton: false,
        timer: _timer
    });
    //_Type = [success,info,warning,question,error]
    Toast.fire({ icon: _type, title: _message })
}
function CheckValidate(formSelector = "") {
    var form = $(formSelector);

    // Kiểm tra tính hợp lệ của form
    if (form[0].checkValidity() === false) {
        
        form.addClass('was-validated');
        return false; // Form không hợp lệ, trả về false
    }

    form.addClass('was-validated');
    return true; // Form hợp lệ, trả về true

}


function showToast(icon, message, timer = 3000) {
    Swal.fire({
        toast: true,
        position: "top-end",
        showConfirmButton: false,
        timer: timer,
        timerProgressBar: true,
        didOpen: (toast) => {
            toast.onmouseenter = Swal.stopTimer;
            toast.onmouseleave = Swal.resumeTimer;
        },
        icon: icon,
        title: message
    });
}

function showAlert(icon, title, text="", showConfirmButton = false, showCancelButton = false, timer = 1500) {
    Swal.fire({
        icon: icon,
        title: title,
        text: text,
        showConfirmButton: showConfirmButton,
        showCancelButton: showCancelButton,
        timer: timer
    });
}

function confirmAction(title = "Bạn có chắc muốn xóa?", icon = "warning", text = "Không thể phục hồi sau khi xóa!", confirmButtonText = "Vâng, hãy xóa", colorConfirm = "#d33") {
    return Swal.fire({
        title: title,
        text: text,
        icon: icon,
        showCancelButton: true,
        confirmButtonColor: colorConfirm,
        cancelButtonColor: "rgb(26,26,26)",
        confirmButtonText: confirmButtonText,
        cancelButtonText: "Hủy"
    }).then((result) => {
        return result.isConfirmed;
    });
}

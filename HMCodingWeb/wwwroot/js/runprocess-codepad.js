document.getElementById("import-code").addEventListener("click", async function () {
    const { value: file } = await Swal.fire({
        title: "Chọn tệp",
        input: "file",
        inputAttributes: {
            "accept": ".cpp,.py,.pas,.js",
            "aria-label": "Tải tệp lên"
        }
    });
    if (file) {
        const reader = new FileReader();
        reader.onload = function (e) {
            const fileContent = e.target.result;
            console.log(fileContent);
            editor.setValue(fileContent);
        };
        reader.readAsText(file);
    };
});

function executeCopy(text) {
    let input = document.createElement('textarea');
    document.body.appendChild(input);
    input.value = text;
    input.select();
    document.execCommand('Copy');
    input.remove();
}
document.getElementById("copy-code").addEventListener("click", function () {
    const button = this;
    button.innerText = "Đã sao chép";
    setTimeout(function () {
        button.innerText = "Sao chép";
    }, 800);
    const content = editor.getValue();
    executeCopy(content);
});
document.getElementById("run").addEventListener("click", function () {
    this.style.display = "none";
    document.querySelector(".loader").style.display = "block";
    let runProcess = {
        "ProgramLanguageId": $("#programlanguage-selector option:selected").attr("data-value"),
        "SourceCode": editor.getValue(),
        "Input": $("#input-data").val(),
        "Output": null,
        "Error": null,
        "InputFile": $("#input-file").val(),
        "OutputFile": $("#output-file").val(),
        "TimeLimit": ($("#time-limit").val() >= 0 ? $("#time-limit").val() : 1)
    }
    $.ajax({
        type: 'POST',
        url: '/CodePad/RunProcessCodepad',
        data: JSON.stringify(runProcess), // Serialize JSON
        contentType: 'application/json; charset=utf-8',
        success: function (response) {
            if (response.isError) {
                let tabTriggerEl = document.querySelector('a[href="#erroralert"]');
                let tab = new bootstrap.Tab(tabTriggerEl);
                tab.show();
                baoloi.setValue(response.error);
                
                showToast("error", "Đã có lỗi xảy ra", 1500);
            }
            else {
                let tabTriggerEl = document.querySelector('a[href="#output"]');
                let tab = new bootstrap.Tab(tabTriggerEl);
                tab.show();
                document.getElementById("output-data").innerHTML = response.output;
                showToast("success", "Đã có kết quả output", 1500);
            }
            $(".run-time").text(response.runTime + " s");
            $(".memory-used").text(response.memoryUsed + " MB");
            document.getElementById("run").style.display = "block";
            document.querySelector(".loader").style.display = "none";
        },
        error: function (err) {
            alert("Error to post request");
            document.getElementById("run").style.display = "block";
            document.querySelector(".loader").style.display = "none";
        }
    });
});
$(document).on('keydown', function (event) {
    if (event.ctrlKey && event.key === 's') {
        event.preventDefault();
        $('#save-as').click();
    }
});;

document.getElementById("save-as").addEventListener("click", async function () {
    var { value: fileName } = await Swal.fire({
        title: "NHẬP TÊN FILE CẦN LƯU",
        input: "text",
        inputAttributes: {
            placeholder: "Nhập tên file ở đây...",
            maxLength: 50
        },
        showCancelButton: true,
        inputValidator: (value) => {
            if (!value) {
                return "Tên file không thể rỗng";
            }
        },
    });
    if (fileName) {
        const confirm = await confirmAction("Bạn có chắc muốn lưu", "", "", "Lưu", "#1f9bcf");

        if (confirm) {
            var model = {
                "FileName": fileName,
                "InputFile": $("#input-file").val(),
                "OutputFile": $("#output-file").val(),
                "ProgramLanguageId": $("#programlanguage-selector option:selected").attr("data-value"),
                "CodeContent": editor.getValue()
            }
            $.ajax({
                url: "/CodePad/SaveAs",
                method: 'POST',
                data: { model: model },
                success: function (response) {
                    if (response.status) {
                        showAlert("success", "Lưu thành công");

                    } else {
                        Swal.fire("Lưu thất bại!", response.error, "error");
                    }
                },
                error: function () {
                    Swal.fire("Lưu thất bại!", "Đã có lỗi xảy ra", "error");
                }
            });



        }
    }
});
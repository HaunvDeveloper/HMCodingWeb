let activeTextarea = null;



$(document).on('focus', 'textarea', function () {
    activeTextarea = this;
})

function parseEscapeSequences(str) {
    return str
        .replace(/\\n/g, '\n')
        .replace(/\\t/g, '\t')
        .replace(/\\r/g, '\r')
        .replace(/\\s/g, ' ')
        .replace(/\\\\/g, '\\');
}

document.getElementById("generateRandomBtn").addEventListener("click", () => {
    if (!activeTextarea) {
        alert("Hãy chọn một nơi nhập trước!");
        return;
    }

    const count = parseInt(document.getElementById("count").value, 10);
    const min = parseInt(document.getElementById("minValue").value, 10);
    const max = parseInt(document.getElementById("maxValue").value, 10);
    let separator = document.getElementById("separator").value;
    const printCount = document.getElementById("printCountCheckbox").checked;

    separator = parseEscapeSequences(separator);

    if (isNaN(count) || isNaN(min) || isNaN(max) || count <= 0) {
        alert("Vui lòng nhập đúng các thông tin.");
        return;
    }

    const numbers = [];
    for (let i = 0; i < count; i++) {
        const rand = Math.floor(Math.random() * (max - min + 1)) + min;
        numbers.push(rand);
    }

    let generatedText = "";
    if (printCount) {
        generatedText = count + "\n" + numbers.join(separator);
    } else {
        generatedText = numbers.join(separator);
    }

    // Chèn tại vị trí con trỏ
    const start = activeTextarea.selectionStart;
    const end = activeTextarea.selectionEnd;
    const text = activeTextarea.value;
    activeTextarea.value = text.slice(0, start) + generatedText + text.slice(end);

    // Đặt lại con trỏ
    activeTextarea.selectionStart = activeTextarea.selectionEnd = start + generatedText.length;

    // Ẩn modal
    const modal = bootstrap.Modal.getInstance(document.getElementById('generateRandomModal'));
    modal.hide();
});


function getProgramLanguageMode(proLanguageCode) {
    if (proLanguageCode == "cpp")
        return "ace/mode/c_cpp";
    else if (proLanguageCode == "py")
        return "ace/mode/python";
    else if (proLanguageCode == "pas")
        return "ace/mode/pascal";
    else
        return "ace/mode/javascript";
}

// Khởi tạo Ace Editor
var sampleEditor = ace.edit("sampleCodeEditor");
sampleEditor.setTheme("ace/theme/monokai");
sampleEditor.session.setMode(getProgramLanguageMode("cpp"));
sampleEditor.setOptions({
    fontSize: "14px",
    enableBasicAutocompletion: true,
    enableSnippets: true,
    enableLiveAutocompletion: true,
    showPrintMargin: false
});

// Khi đổi ngôn ngữ
document.getElementById("languageSelect").addEventListener("change", function () {
    var lang = this.value;
    sampleEditor.session.setMode(getProgramLanguageMode(lang));
});

// Khi tải file
document.getElementById("codeFileUpload").addEventListener("change", function () {
    var file = this.files[0];
    if (!file) return;
    var reader = new FileReader();
    reader.onload = function (e) {
        sampleEditor.setValue(e.target.result, -1);
    };
    reader.readAsText(file);
});

$('#insertSampleCodeBtn').on('click', function () {
    var codeContent = sampleEditor.getValue();
    if (!codeContent) {
        Swal.fire({
            icon: 'error',
            title: 'Lỗi',
            text: 'Vui lòng nhập mã nguồn hoặc tải tệp lên.'
        });
        return;
    }
    var payLoad = {
        ProgramLanguageId: $("#languageSelect option:selected").attr("data-value"),
        SourceCode: codeContent,
        TimeLimit: $("#RuntimeLimit").val(),
        SampleOutputs: []
    }
    $('#testcase-row-list').find('tr').each(function (index, element) {
        var detail = {
            Input: $(element).find('.test-case-input').val(),
            Output: $(element).find('.test-case-output').val(),
            SampleIndex: $(element).data('value')
        }
        payLoad.SampleOutputs.push(detail);
    });
    Swal.fire({
        title: 'Đang xử lý...',
        text: 'Vui lòng chờ giây lát',
        allowOutsideClick: false,
        didOpen: () => {
            Swal.showLoading();
        }
    });
    $.ajax({
        url: '/Exercise/GenerateOutput',
        method: 'post',
        data: JSON.stringify(payLoad), // Serialize JSON
        contentType: 'application/json; charset=utf-8',
        success: function (response) {
            Swal.close();
            if (response.status) {
                var data = response.data;
                if (data.isError) {
                    showToast('error', 'Có lỗi');
                } else {
                    showToast('success', 'Thành công');
                }
                // Cập nhật các ô output trong bảng test case
                $('#testcase-row-list').find('tr').each(function (index, element) {
                    var outputItem = response.data.sampleOutputs[index];  // <-- Sửa ở đây
                    if (outputItem) {
                        if (outputItem.isError) {
                            $(element).find('.test-case-output').val(outputItem.error);
                        } else {
                            $(element).find('.test-case-output').val(outputItem.output);
                        }
                    } else {
                        $(element).find('.test-case-output').val('Không có dữ liệu trả về');
                    }
                });

            } else {
                Swal.fire("Lỗi!", response.error, "error");
            }
            //hide modal
            $('#sampleCodeModal').modal('hide');
        },
        error: function (error) {
            Swal.close();
            Swal.fire("Lỗi!", error, "error");
            $('#sampleCodeModal').modal('hide');
        }
    });
});

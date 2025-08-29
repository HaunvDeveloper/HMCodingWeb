const sessionCode = Math.random().toString(36).substring(2, 15);
$(document).ready(function () {
    // Initialize SignalR connection
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/markingHub")
        .configureLogging(signalR.LogLevel.Information)
        .build();

    // Start SignalR connection
    async function startSignalR() {
        try {
            await connection.start();
            console.log("SignalR Connected.");
        } catch (err) {
            console.error("SignalR Connection Error: ", err);
            setTimeout(startSignalR, 5000); // Retry after 5 seconds
        }
    }

    // Handle connection closed event
    connection.onclose(async () => {
        console.warn("SignalR connection closed. Attempting to reconnect...");
        await startSignalR();
    });

    connection.on("ReceiveTestCaseResult", function (testCaseResult) {
        //console.log("Received test case result: ", testCaseResult);
        if (testCaseResult.sessionCode != sessionCode) {
            return;
        }
        // Determine failure reason
        let failureReason = '';
        if (!testCaseResult.isCorrect) {
            if (testCaseResult.isTimeLimitExceed) {
                failureReason = 'Vượt quá thời gian';
            } else if (testCaseResult.isMemoryLimitExceed) {
                failureReason = 'Vượt quá dung lượng';
            } else if (testCaseResult.isError) {
                failureReason = 'Lỗi thực thi';
            } else {
                failureReason = 'Kết quả sai';
            }
        }

        // Generate result HTML
        const resultHtml = `
                    <div class="test-case-no-${testCaseResult.testCaseIndex} test-case-result ${testCaseResult.isCorrect ? 'passed' : 'failed'} ">
                        <div class="d-flex align-items-center">
                            <span class="status-icon">
                                ${testCaseResult.isCorrect ? '✅' : '❌'}
                            </span>
                            <strong class="text-dark">Test Case ${testCaseResult.testCaseIndex}:</strong>
                            <span class="ms-2 ${testCaseResult.isCorrect ? 'text-success' : 'text-danger'}">
                                <b>${testCaseResult.isCorrect ? 'Đạt' : 'Thất bại'}</b>
                            </span>
                        </div>
                        ${failureReason ? `<div class="details"><strong>Lý do:</strong> ${failureReason}</div>` : ''}
                        <div class="details"><strong>Thời gian:</strong> ${testCaseResult.runTime.toFixed(6)}s</div>
                        ${testCaseResult.memoryUsed ? `<div class="details"><strong>Bộ nhớ sử dụng:</strong> ${testCaseResult.memoryUsed.toFixed(2)} MB</div>` : ''}
                    </div>
                `;
        $("#test-case-results").append(resultHtml);

        if (testCaseResult.testCaseIndex < testCaseResult.totalTestcase) {
            var testCaseDiv = document.querySelector(`#test-case-results`);
            testCaseDiv.scrollTo({
                top: testCaseDiv.scrollHeight,
                behavior: "smooth" // thêm hiệu ứng mượt
            });
        }
        else {
            //scroll to 0
            var testCaseDiv = document.querySelector(`#test-case-results`);
            testCaseDiv.scrollTo({
                top: 0,
                behavior: "smooth" // thêm hiệu ứng mượt
            });
        }


        // Update progress bar
        const progressBar = $('.progress-bar');
        const progress = (testCaseResult.testCaseIndex / testCaseResult.totalTestcase) * 100;
        progressBar.css('width', `${progress}%`);
        progressBar.attr('aria-valuenow', progress);


        // Handle errors (switch to error tab)
        if (testCaseResult.isError) {
            const tabTriggerEl = document.querySelector('a[href="#erroralert"]');
            if (tabTriggerEl) {
                const tab = new bootstrap.Tab(tabTriggerEl);
                tab.show();
            }
            if (baoloi && typeof baoloi.setValue === 'function') {
                baoloi.setValue(testCaseResult.errorContent || '');
            }
            $(".run-time").val(testCaseResult.runTime);
            showToast("error", "Đã có lỗi xảy ra", 1500);
        }


    });

    // Utility function to escape HTML
    function escapeHtml(unsafe) {
        return unsafe
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }

    // Start SignalR connection
    startSignalR();

    // Handle marking button click
    $("#btn-marking").on("click", async function () {
        // Disable button
        $(this).prop("disabled", true).text("Đang chấm...");

        // Clear previous results
        $("#test-case-results").empty();
        $('.progress').css('display', 'flex');
        $('.progress-bar').css('width', `0%`);
        $('.progress-bar').attr('aria-valuenow', 0);
        try {

            await $.ajax({
                url: "/Marking/Marking",
                type: "POST",
                data: {
                    ExerciseId: exerciseId,
                    ProgramLanguageId: $("#programlanguage-selector option:selected").attr("data-value"),
                    SourceCode: editor.getValue(),
                    SessionCode: sessionCode,
                },
                success: function (response) {
                    if (response.status) {
                        var statusContent = 'Tất cả đúng';
                        if (!response.data.isAllCorrect) {
                            if (response.data.status === 'TLE') {
                                statusContent = 'Vượt quá thời gian';
                            } else if (response.data.status === 'MLE') {
                                statusContent = 'Vượt quá dung lượng';
                            } else if (response.data.status === 'RE') {
                                statusContent = 'Lỗi thực thi';
                            } else {
                                statusContent = 'Có test case sai';
                            }
                        }
                        const resultHtml = `
                                    <div class="finalresult alert ${response.data.isAllCorrect ? 'alert-success' : 'alert-warning'}">
                                        <h5>Kết quả chấm điểm:</h5>
                                        <p><strong>Trạng thái:</strong> ${statusContent}</p>
                                        <p><strong>Điểm số:</strong> ${response.data.score}/100</p>
                                        ${response.data.isError ? '<p><strong>Lỗi:</strong> Có lỗi trong quá trình chạy chương trình</p>' : ''}
                                        ${response.data.pointGain ? `<p><strong>Bạn được nhận thêm:</strong> ${response.data.pointGain} điểm</p>` : ''}
                                        <p><a href="/Marking/ViewDetailMarking/${response.data.newId}">Xem chi tiết</a></p>
                                    </div>
                                `;
                        $("#test-case-results").prepend(resultHtml); // Prepend instead of append
                        $("#btn-marking").prop("disabled", false).text("CHẤM ĐIỂM");
                        document.querySelector('.finalresult').scrollIntoView({
                            behavior: 'smooth', // 'auto' hoặc 'smooth'
                            block: 'center'     // 'start', 'center', 'end', 'nearest'
                        });
                        $('.progress').css('display', 'none');
                    } else {
                        const errorHtml = `
                                    <div class="alert alert-danger">
                                        <strong>Lỗi:</strong> ${response.message || "Gửi bài chấm điểm thất bại."}
                                    </div>
                                `;
                        $("#test-case-results").prepend(errorHtml);
                        $("#btn-marking").prop("disabled", false).text("CHẤM ĐIỂM");
                        $('.progress').css('display', 'none');
                    }
                },
                error: function (xhr) {
                    console.error("Error submitting code: ", xhr.responseText);
                    $("#test-case-results").append(`<div class="alert alert-danger">Error: ${xhr.responseText}</div>`);
                    $("#btn-marking").prop("disabled", false).text("CHẤM ĐIỂM");
                    $('.progress').css('display', 'none');
                }
            });
        } catch (err) {
            console.error("AJAX Error: ", err);
            $("#test-case-results").append(`<div class="alert alert-danger">Error: ${err.message}</div>`);
            $("#btn-marking").prop("disabled", false).text("CHẤM ĐIỂM");
            $('.progress').css('display', 'none');
        }
    });
});
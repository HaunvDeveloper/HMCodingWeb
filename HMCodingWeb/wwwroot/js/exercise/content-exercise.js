


$(document).ready(function () {

    const host = document.querySelector('#inner-content');
    const shadowRoot = host.attachShadow({ mode: 'open' });
    const innerContent = `
                    <style>
                        strong,b{
                            font-weight: 900;
                        }
                    </style>
                    <div id="dependency" style="color:black;">
                        ${host.innerHTML}
                    </div>
                `;
    shadowRoot.innerHTML = innerContent;

    var localStorageKey = `saveCodepadExercise${exerciseId}_${currentUserId}`;
    let type = getParamFromCurrentUrl("tab");
    if (type === "code") {
        $("#input-tab").prop("checked", true);
        swapTab();
    } else {
        $("#input-tab").prop("checked", false);
        swapTab();
    }
    editor.getSession().on('change', function () {
        localStorage.setItem(localStorageKey, editor.getValue());
    });
    var storedContent = localStorage.getItem(localStorageKey);
    var initSourceCode = getParamFromCurrentUrl('viewCode');
    if (storedContent && initSourceCode !== "True") {
        editor.setValue(storedContent, -1);
    }


    $("#input-tab").on('click', swapTab);

    function swapTab() {
        const checked = $("#input-tab").prop("checked");
        if (checked) {
            addParamToCurrentUrl("tab", "code");
            $("#view-title").removeClass("active");
            $("#code-title").addClass("active");
            $("#view-content-section").slideUp(300, function () {
                $("#input-tab").addClass("d-none");
                $("#code-section").removeClass("d-none").slideDown(300);
                document.querySelector('#editor').scrollIntoView({
                    behavior: 'smooth', // 'auto' hoặc 'smooth'
                    block: 'center'     // 'start', 'center', 'end', 'nearest'
                });
            });

        } else {
            addParamToCurrentUrl("tab", "view");
            $("#view-title").addClass("active");
            $("#code-title").removeClass("active");
            $("#code-section").slideUp(300, function () {
                $("#input-tab").addClass("d-none");
                $("#view-content-section").removeClass("d-none").slideDown(300);
                document.querySelector('#content-container').scrollIntoView({
                    behavior: 'smooth', // 'auto' hoặc 'smooth'
                    block: 'center'     // 'start', 'center', 'end', 'nearest'
                });
            });

        }
    }
});
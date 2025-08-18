
CKEDITOR.plugins.add('insertCustomHtml', {
    icons: 'insertCustomHtml',
    init: function (editor) {
        editor.ui.addButton('InsertCustomHtml', {
            label: 'Chèn bảng mẫu',
            command: 'insertHtmlCommand',
            toolbar: 'insert',
            icon: '/assets/icons/insert-table.svg' // hoặc icon tuỳ chọn
        });

        editor.addCommand('insertHtmlCommand', {
            exec: function (editor) {
                const htmlContent = `
                            <table align="center" border="1" cellpadding="1" cellspacing="1" style="width:500px">
                                <tbody>
                                    <tr>
                                        <td style="text-align:center"><strong>INPUT&nbsp;&nbsp;</strong></td>
                                        <td style="text-align:center"><strong>OUTPUT</strong></td>
                                    </tr>
                                    <tr>
                                        <td></td>
                                        <td></td>
                                    </tr>
                                    <tr>
                                        <td></td>
                                        <td></td>
                                    </tr>
                                </tbody>
                            </table>
                        `;
                editor.insertHtml(htmlContent);
            }
        });
    }
});

CKEDITOR.replace('ex-content', {
    extraPlugins: 'justify,table,link,insertCustomHtml',
    height: 600,
    toolbar: [
        { name: 'document', items: ['Source', '-', 'NewPage', 'Preview', 'Print', '-', 'Templates'] },
        { name: 'clipboard', items: ['Cut', 'Copy', 'Paste', 'PasteText', 'PasteFromWord', '-', 'Undo', 'Redo'] },
        { name: 'editing', items: ['Find', 'Replace', '-', 'SelectAll', '-', 'SpellChecker', 'Scayt'] },
        { name: 'forms', items: ['Form', 'Checkbox', 'Radio', 'TextField', 'Textarea', 'Select', 'Button', 'ImageButton', 'HiddenField'] },
        '/',
        { name: 'basicstyles', items: ['Bold', 'Italic', 'Underline', 'Strike', 'Subscript', 'Superscript', '-', 'RemoveFormat'] },
        { name: 'paragraph', items: ['NumberedList', 'BulletedList', '-', 'Outdent', 'Indent', '-', 'Blockquote', 'CreateDiv', '-', 'JustifyLeft', 'JustifyCenter', 'JustifyRight', 'JustifyBlock', '-', 'BidiLtr', 'BidiRtl', 'Language'] },
        { name: 'links', items: ['Link', 'Unlink', 'Anchor'] },
        { name: 'insert', items: ['Image', 'Flash', 'Table', 'HorizontalRule', 'Smiley', 'SpecialChar', 'PageBreak', 'Iframe', 'InsertCustomHtml'] },
        '/',
        { name: 'styles', items: ['Styles', 'Format', 'Font', 'FontSize'] },
        { name: 'colors', items: ['TextColor', 'BGColor'] },
        { name: 'tools', items: ['Maximize', 'ShowBlocks'] },
    ]
});

CKEDITOR.instances['ex-content'].on('change', function () {
    updatePreview();
    saveDataToLocalStorage();
});
function saveDataToLocalStorage() {
    var editorData = CKEDITOR.instances['ex-content'].getData();
    localStorage.setItem('editor_data', editorData);
}
function updatePreview() {
    var content = CKEDITOR.instances['ex-content'].getData();
    const host = document.querySelector('#preview');
    host.innerHTML = ''; // Clear any non-shadow content (if any)

    // Get or create shadow root
    let shadowRoot = host.shadowRoot || host.attachShadow({ mode: 'open' });

    // Update shadow content
    const innerContent = `
             <style>
                        strong,b{
                            font-weight: 900;
                        }
                    </style>
                <div id="dependency" style="color:black">
                    ${content || '<p>Chưa có nội dung.</p>'}
                </div>`;
    shadowRoot.innerHTML = innerContent;
}
$(document).ready(function () {
    var storedData = localStorage.getItem('editor_data');
    var initialContent = CKEDITOR.instances['ex-content'].getData();
    if (storedData && !initialContent) {
        CKEDITOR.instances['ex-content'].setData(storedData);
        updatePreview();
    }

});
function removeCKENotification() {
    try {
        $('.cke_notifications_area').remove();

        if ($('.cke_notifications_area')) {
            setTimeout(removeCKENotification, 200);
        }
    }
    catch (e) {
        setTimeout(removeCKENotification, 200);
    }
}
removeCKENotification();
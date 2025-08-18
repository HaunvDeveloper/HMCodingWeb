// CODE SECTION
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

var editor = ace.edit("editor");
editor.setTheme($('#theme-selector').val());
editor.session.setMode(getProgramLanguageMode($('#programlanguage-selector').val()));
ace.require("ace/ext/language_tools");
editor.setOptions({
    autoScrollEditorIntoView: true,
    copyWithEmptySelection: true,
    enableBasicAutocompletion: true,
    enableLiveAutocompletion: true,
    enableSnippets: true
});
$('#theme-selector').on('change', function (event) {
    const theme = event.target.value;
    editor.setTheme(theme);
});
$('#fontsize-selector').on('change', function (event) {
    var fontSize = $(this).val();
    editor.setFontSize(fontSize + "px");
});
$('#programlanguage-selector').on('change', function (event) {
    editor.session.setMode(getProgramLanguageMode($(this).val()));
});


var baoloi = ace.edit("error");
baoloi.setTheme("ace/theme/kuroir");
baoloi.setShowPrintMargin(false);
baoloi.setDisplayIndentGuides(false);
baoloi.setReadOnly(true);
baoloi.getSession().setMode("ace/mode/c_cpp");
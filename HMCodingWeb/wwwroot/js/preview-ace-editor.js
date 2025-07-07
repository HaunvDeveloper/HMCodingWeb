$(document).ready(function () {
    // Khởi tạo Ace Editor
    var editor = ace.edit("codePreviewEditor");
    editor.setTheme("ace/theme/monokai");
    editor.session.setMode("ace/mode/javascript");
    editor.setValue(`// Chọn ngôn ngữ để xem code mẫu\nconsole.log("Hello, World!");`, -1);
    editor.setOptions({
        readOnly: true,
        highlightActiveLine: false,
        highlightGutterLine: false,
        showPrintMargin: false,
        fontSize: '16px'
    });
    editor.renderer.$cursorLayer.element.style.display = "none";

    // Mapping ngôn ngữ
    const languageModes = {
        cpp: "c_cpp",
        py: "python",
        pascal: "pascal",
        js: "javascript"
    };

    // Mã code mẫu
    const sampleCodes = {
        cpp: `// C++ sample\n#include<iostream>\nusing namespace std;\nint main() {\n    cout << "Hello C++";\n    return 0;\n}`,
        py: `# Python sample\nprint("Hello Python")`,
        pas: `// Pascal sample\nprogram HelloWorld;\nbegin\n    writeln('Hello Pascal');\nend.`,
        js: `// JavaScript sample\nconsole.log("Hello JavaScript");`
    };

    // Khi đổi Theme
    $("#ThemeCodeId").change(function () {
        const theme = $(this).find("option:selected").text().trim().toLowerCase();
        editor.setTheme("ace/theme/" + theme);
    });

    $("#ThemeCodeId").trigger("change"); // Khởi tạo với theme đầu tiên)


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

    // Khi đổi Ngôn ngữ
    $("#ProgramLanguageId").change(function () {
        // Lấy text (vd: cpp, py)
        const langKey = $(this).find("option:selected").attr('data-code');
        const mode = getProgramLanguageMode(langKey) || "text";
        editor.session.setMode(mode);
        editor.setValue(sampleCodes[langKey] || "// No sample code", -1);
    });

    $("#ProgramLanguageId").trigger("change"); // Khởi tạo với ngôn ngữ đầu tiên)
});

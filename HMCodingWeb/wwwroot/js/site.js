function addParamToCurrentUrl(key, value) {
    const urlObj = new URL(window.location.href);

    if (
        value === null ||
        value === undefined ||
        value === '' ||
        (typeof value === 'number' && isNaN(value))
    ) {
        urlObj.searchParams.delete(key); // xoá param nếu không hợp lệ
    } else {
        urlObj.searchParams.set(key, value);
    }

    const newUrl = urlObj.toString();
    window.history.replaceState({}, '', newUrl);
    return newUrl;
}


function getParamFromCurrentUrl(key) {
    const urlObj = new URL(window.location.href);
    return urlObj.searchParams.get(key);
}
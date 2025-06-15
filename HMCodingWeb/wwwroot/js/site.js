function addParamToCurrentUrl(key, value) {
    const urlObj = new URL(window.location.href);
    urlObj.searchParams.set(key, value);
    window.history.replaceState({}, '', urlObj.toString());
    return urlObj.toString();
}

function getParamFromCurrentUrl(key) {
    const urlObj = new URL(window.location.href);
    return urlObj.searchParams.get(key);
}
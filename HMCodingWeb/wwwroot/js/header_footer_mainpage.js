const openButton = document.getElementById('openSideBar');
const closeButton = document.getElementById('closeSideBar');
const sidebar = document.getElementById('side-bar');
const content = document.getElementById('content');
const titlesite = document.getElementById('title-site');
const footer = document.getElementById('footer');
const lightmood = document.getElementById('lightmood')
const darkmood = document.getElementById('darkmood')



// Mở side-bar
openButton.addEventListener('click', function(){
    sidebar.style.display = 'block';
});

// Đóng side-bar
closeButton.addEventListener('click', function(){
    sidebar.style.display = 'none';
});
header.addEventListener('click', function(){
    sidebar.style.display = 'none';
});
footer.addEventListener('click', function(){
    sidebar.style.display = 'none';
});
content.addEventListener('click', function(){
    sidebar.style.display = 'none';
});
// bật c
//document.addEventListener("DOMContentLoaded", () => {
//    const printButtons = document.querySelectorAll(".printBtn");

//    printButtons.forEach(btn => {
//        btn.addEventListener("click", () => {
//            const type = btn.getAttribute("data-type"); // e.g., "Bento", "Curry"
//            const today = new Date().toLocaleDateString();

//            $('#printArea').printThis({
//                importCSS: true,
//                loadCSS: "/css/site.css",
//                header: `<h2>${type}</h2><p>Date: ${today}</p><hr />`
//            });
//        });
//    });
//});
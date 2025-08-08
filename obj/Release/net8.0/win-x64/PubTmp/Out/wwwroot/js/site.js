



//Dismiss dashboard alert
document.addEventListener('DOMContentLoaded', function () {
    const alert = document.querySelector('.alert-dismissible');

    if (alert) {
        setTimeout(() => {
            // Bootstrap 5: programmatically dismiss the alert
            const alertInstance = bootstrap.Alert.getOrCreateInstance(alert);
            alertInstance.close();
        }, 2000); // 2000ms = 2 seconds
    }

    //Update Firstname, lastname, section in Resevation/Order Form
    document.getElementById("employeeId").addEventListener("change", function () {
        const empId = this.value;

        if (empId.trim() === "") return;

        const baseUrl = '@Url.Content("~/")'.replace(/\/+$/, '');
        fetch(`${baseUrl}/Home/GetEmployeeById?id=${encodeURIComponent(empId)}`)
            .then(response => {
                if (!response.ok) throw new Error("Employee not found.");
                return response.json();
            })
            .then(data => {
                document.getElementById("firstName").value = data.firstName;
                document.getElementById("lastName").value = data.lastName;
                document.getElementById("section").value = data.section;
            })
            .catch(error => {
                alert("Employee not found.");
                document.getElementById("firstName").value = "";
                document.getElementById("lastName").value = "";
                document.getElementById("section").value = "";
            });
    });
});


//Update Ordername and image
$(document).ready(function () {
    $('.order-now-btn').on('click', function () {
        const menuId = $(this).data('menu-id');
        const menuName = $(this).data('menu-name');
        const menuImage = $(this).data('menu-image');
        const availabilityDate = this.getAttribute('data-availability-date');

        $('#menuId').val(menuId);
        $('#menuName').val(menuName);
        $('#menuImage').attr('src', menuImage);
        $('#menuImagePath').val(menuImage);
        document.getElementById('displayDate').value = availabilityDate;

        const empId = this.value;

        if (!empId || empId.trim() === "") return;

        fetch(`/Home/GetEmployeeById?id=${encodeURIComponent(empId)}`)
            .then(response => {
                if (!response.ok) throw new Error("Employee not found.");
                return response.json();
            })
            .then(data => {
                document.getElementById("employeeId").value = data.empId;
                document.getElementById("firstName").value = data.firstName;
                document.getElementById("lastName").value = data.lastName;
                document.getElementById("section").value = data.section;
            })
            .catch(error => {
                alert("Employee not found.");
                document.getElementById("employeeId").value = "";
                document.getElementById("firstName").value = "";
                document.getElementById("lastName").value = "";
                document.getElementById("section").value = "";
            });
    });
});

if (menuImage) {
    $('#menuImage').attr('src', menuImage).show();
} else {
    $('#menuImage').hide();
}
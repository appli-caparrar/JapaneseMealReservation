//document.addEventListener('DOMContentLoaded', function () {
//    const employeeId = document.getElementById('employeeId').value.trim();

//    if (employeeId) {
//        fetch(`/Reservation/GetEmployeeInfo?employeeId=${encodeURIComponent(employeeId)}`)
//            .then(response => {
//                if (!response.ok) throw new Error('Employee not found');
//                return response.json();
//            })
//            .then(data => {
//                document.getElementById('firstName').value = data.firstName || '';
//                document.getElementById('lastName').value = data.lastName || '';
//                document.getElementById('section').value = data.section || '';
//            })
//            .catch(err => {
//                console.error('Fetch error:', err);
//                alert("Failed to load employee info.");
//            });
//    }
//});
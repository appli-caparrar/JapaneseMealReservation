

document.addEventListener('DOMContentLoaded', function () {
    const form = document.getElementById('advanceReservationForm');
    if (!form) {
        console.error("Form with ID 'reservationForm' not found.");
        return;
    }

    form.addEventListener('submit', function (e) {
        e.preventDefault();

        const get = id => {
            const el = document.getElementById(id);
            if (!el) {
                console.error(`Element with ID '${id}' not found.`);
                throw new Error(`Missing element: ${id}`);
            }
            return el;
        };

        try {
            const data = {
                EmployeeId: get('employeeId').value,
                FirstName: get('firstName').value,
                LastName: get('lastName').value,
                Section: get('section').value,
                CustomerType: get('customerType').value,
                ReservationDate: get('selectedDate').value,
                MealTime: get('time').value,
                MenuType: get('menu').value,
                Quantity: parseInt(get('quantity').value)
            };

            const tokenEl = document.querySelector('[name="__RequestVerificationToken"]');
            if (!tokenEl) {
                console.error("CSRF token not found.");
                return;
            }

            const token = tokenEl.value;

            fetch('../Reservation/SaveAdvanceOrder', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify(data)
            })
                .then(res => res.json())
                .then(res => {
                    if (res.success) {

                        Swal.fire({
                            icon: 'success',
                            title: 'Reserved!',
                            text: res.message,
                        }).then(() => {
                            form.reset();
                        });
                    } else {
                        console.error(res.errors || res.message);
                        Swal.fire('Error', res.message, 'error');
                    }
                })
                .catch(err => {
                    console.error(err);
                    Swal.fire('Request failed', err.message, 'error');
                });
        } catch (error) {
            Swal.fire('Error', error.message, 'error');
        }
    });
});


//document.getElementById('reservationForm').addEventListener('submit', function (e) {
//    e.preventDefault();

//    const data = {
//        EmployeeId: document.getElementById('employeeId').value,
//        FirstName: document.getElementById('firstName').value,
//        LastName: document.getElementById('lastName').value,
//        Section: document.getElementById('section').value,
//        CustomerType: document.getElementById('costumerType').value,
//        ReservationDate: document.getElementById('selectedDate').value,
//        MealTime: document.getElementById('time').value,
//        MenuType: document.getElementById('menu').value,
//        Quantity: parseInt(document.getElementById('quantity').value)
//    };

//    const token = document.querySelector('[name="__RequestVerificationToken"]').value;

//    fetch('../Reservation/SaveAdvanceOrder', {
//        method: 'POST',
//        headers: {
//            'Content-Type': 'application/json',
//            'RequestVerificationToken': token
//        },
//        body: JSON.stringify(data)
//    })
//        .then(res => res.json())
//        .then(res => {
//            if (res.success) {
//                Swal.fire({
//                    icon: 'success',
//                    title: 'Reserved!',
//                    text: res.message,
//                }).then(() => {
//                    document.getElementById('reservationForm').reset();
//                });
//            } else {
//                console.error(res.errors || res.message);
//                Swal.fire('Error', res.message, 'error');
//            }
//        })
//        .catch(err => {
//            console.error(err);
//            Swal.fire('Request failed', err.message, 'error');
//        });
//});

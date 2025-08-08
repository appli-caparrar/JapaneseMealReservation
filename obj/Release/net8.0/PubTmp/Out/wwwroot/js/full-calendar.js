

//document.addEventListener('DOMContentLoaded', function () {
//    const calendarEl = document.getElementById('calendar');

//    const calendar = new FullCalendar.Calendar(calendarEl, {
//        initialView: 'dayGridMonth',
//        selectable: true,
//        selectAllow: function (selectInfo) {
//            // Prevent selecting more than one day
//            const duration = selectInfo.end - selectInfo.start;
//            return duration <= 86400000; // 1 day in ms
//        },
//        select: function (info) {
//            const selectedDate = info.startStr;
//            alert(`You selected: ${selectedDate}`);

//            // Optional: Add event on selected day
//            calendar.addEvent({
//                title: 'Your Reservation',
//                start: selectedDate,
//                allDay: true
//            });
//        }
//    });

//    calendar.render();
//});

//document.addEventListener('DOMContentLoaded', function () {
//    const calendarEl = document.getElementById('calendar');
//    const modal = new bootstrap.Modal(document.getElementById('reservationModal'));

//    const calendar = new FullCalendar.Calendar(calendarEl, {
//        initialView: 'dayGridMonth',
//        selectable: true,
//        dateClick: function (info) {
//            // Store selected date
//            document.getElementById('selectedDate').value = info.dateStr;
//            // Open modal
//            modal.show();
//        }
//    });

//    calendar.render();

//    // Handle form submission
//    document.querySelector('#reservationModal form').addEventListener('submit', function (e) {
//        e.preventDefault();
//        const name = document.getElementById('name').value;
//        const note = document.getElementById('note').value;
//        const date = document.getElementById('selectedDate').value;

//        // Add reservation to calendar
//        calendar.addEvent({
//            title: name,
//            start: date,
//            allDay: true,
//            description: note
//        });

//        modal.hide();
//        this.reset();
//    });
//});

//document.addEventListener('DOMContentLoaded', function () {
//    const calendarEl = document.getElementById('calendar');
//    const modal = new bootstrap.Modal(document.getElementById('reservationModal'));

//    const displayDate = document.getElementById('displayDate');
//    const selectedDateInput = document.getElementById('selectedDate');
//    const menuSelect = document.getElementById('menu');

//    const weekdayMenus = {
//        1: ['Bento', 'Maki'],        // Monday
//        2: ['Bento', 'Noodles'],     // Tuesday
//        3: ['Bento', 'Maki'],        // Wednesday
//        4: ['Bento', 'Curry'],       // Thursday
//        5: ['Bento', 'Noodles'],      // Friday
//        6: ['Bento']      // Saturday
//    };

//    const calendar = new FullCalendar.Calendar(calendarEl, {
//        initialView: 'dayGridMonth',
//        selectable: true,
//        dateClick: function (info) {
//            const dateStr = info.dateStr;
//            const date = new Date(dateStr);
//            const day = date.getDay(); // 0 = Sunday, 1 = Monday, ..., 6 = Saturday

//            // Display and store selected date
//            displayDate.value = dateStr;
//            selectedDateInput.value = dateStr;

//            // Reset and populate menu options
//            menuSelect.innerHTML = '<option value="">-- Select Menu --</option>';

//            if (weekdayMenus[day]) {
//                weekdayMenus[day].forEach(menuItem => {
//                    const option = document.createElement('option');
//                    option.value = menuItem;
//                    option.textContent = menuItem;
//                    menuSelect.appendChild(option);
//                });
//                menuSelect.disabled = false;
//            } else {
//                const option = document.createElement('option');
//                option.value = "";
//                option.textContent = "No menu available";
//                option.disabled = true;
//                menuSelect.appendChild(option);
//                menuSelect.disabled = true;
//            }

//            // Show modal
//            modal.show();
//        }
//    });

//    calendar.render();

//    // Handle form submit
//    document.getElementById('reservationForm').addEventListener('submit', function (e) {
//        e.preventDefault();

//        const date = document.getElementById('selectedDate').value;
//        const empId = document.getElementById('employeeId').value;
//        const firstName = document.getElementById('firstName').value;
//        const lastName = document.getElementById('lastName').value;
//        const section = document.getElementById('section').value;
//        const quantity = document.getElementById('quantity').value;
//        const time = document.getElementById('time').value;
//        const menu = menuSelect.value;

//        // Add reservation to calendar
//        calendar.addEvent({
//            title: `${firstName} ${lastName} (${menu} x${quantity})`,
//            start: date + 'T' + time,
//            allDay: false,
//            extendedProps: {
//                employeeId: empId,
//                section: section,
//                quantity: quantity,
//                menu: menu
//            }
//        });

//        modal.hide();
//        this.reset();
//    });
//});


document.addEventListener('DOMContentLoaded', function () {
    const calendarEl = document.getElementById('calendar');
/*    const modal = new bootstrap.Modal(document.getElementById('reservationModal'));*/

    const displayDate = document.getElementById('selectedDate');
    const selectedDateInput = document.getElementById('selectedDate');
    const menuSelect = document.getElementById('menu');

    const weekdayMenus = {
        1: ['Bento', 'Maki'],        // Monday
        2: ['Bento', 'Noodles'],     // Tuesday
        3: ['Bento', 'Maki'],        // Wednesday
        4: ['Bento', 'Curry'],       // Thursday
        5: ['Bento', 'Noodles'],     // Friday
        6: ['Bento']               // Saturday
    };

    const events = [];

    //// Populate events automatically based on weekdayMenus
    //const today = new Date();
    //const startDate = new Date(today.getFullYear(), today.getMonth(), 1); // first day of current month
    //const endDate = new Date(today.getFullYear(), today.getMonth() + 1, 0); // last day of current month

    //for (let date = new Date(startDate); date <= endDate; date.setDate(date.getDate() + 1)) {
    //    const day = date.getDay(); // 0 = Sunday, 1 = Monday, ..., 6 = Saturday

    //    if (weekdayMenus[day]) {
    //        weekdayMenus[day].forEach(menuItem => {
    //            events.push({
    //                title: menuItem,
    //                start: new Date(date),
    //                allDay: true,
    //                backgroundColor: '#529DDA',
    //                borderColor: '#36658C'
    //            });
    //        });
    //    }
    //}

    //populate the Menu and Date when click cell date
    const calendar = new FullCalendar.Calendar(calendarEl, {
        initialView: 'dayGridMonth',
        height: "auto",
        selectable: true,
        events: events,
        dayCellDidMount: function (arg) {
            const date = new Date(arg.date);
            const weekday = date.getDay();
            const menus = weekdayMenus[weekday];
            const cell = arg.el;

            // Disable Sundays
            if (weekday === 0) {
                cell.style.backgroundColor = "#E1E1E1";
                cell.style.pointerEvents = "none";
                return;
            }

            // Add plain text menu items (no checkboxes)
            if (menus) {
                menus.forEach(menu => {
                    const menuText = document.createElement('div');
                    menuText.classList.add('small', 'text-primary'); // Add styling if you want
                    menuText.textContent = menu;
                    cell.appendChild(menuText);
                });
            }
        },
        dateClick: function (info) {
            const dateStr = info.dateStr;
            const date = new Date(dateStr);
            const day = date.getDay();

            
            // Display and store selected date
            displayDate.value = dateStr;
            selectedDateInput.value = dateStr;

            // Reset and populate menu options
            menuSelect.innerHTML = '<option value="">-- Select Menu --</option>';

            if (weekdayMenus[day]) {
                weekdayMenus[day].forEach(menuItem => {
                    const option = document.createElement('option');
                    option.value = menuItem;
                    option.textContent = menuItem;
                    menuSelect.appendChild(option);
                });
                menuSelect.disabled = false;
            } else {
                const option = document.createElement('option');
                option.value = "";
                option.textContent = "No menu available";
                option.disabled = true;
                menuSelect.appendChild(option);
                menuSelect.disabled = true;
            }

            // Show modal
           /* modal.show();*/
        }
    });

    //Change Menu options when change the date from input type=date
    displayDate.addEventListener('change', function () {
        const dateStr = this.value;
        const date = new Date(dateStr);
        const day = date.getDay(); // 0 = Sunday, 1 = Monday, ..., 6 = Saturday

        selectedDateInput.value = dateStr;

        // Prevent Sunday selection
        if (day === 0) {
            alert("Reservations are not allowed on Sundays.");
            displayDate.value = '';
            selectedDateInput.value = '';
            menuSelect.innerHTML = '<option value="">-- Select Menu --</option>';
            menuSelect.disabled = true;
            return;
        }

        // Reset and populate menu options
        menuSelect.innerHTML = '<option value="">-- Select Menu --</option>';

        if (weekdayMenus[day]) {
            weekdayMenus[day].forEach(menuItem => {
                const option = document.createElement('option');
                option.value = menuItem;
                option.textContent = menuItem;
                menuSelect.appendChild(option);
            });
            menuSelect.disabled = false;
        } else {
            const option = document.createElement('option');
            option.value = "";
            option.textContent = "No menu available";
            option.disabled = true;
            menuSelect.appendChild(option);
            menuSelect.disabled = true;
        }
    });

    calendar.render();

    // Handle form submit
    document.getElementById('reservationForm').addEventListener('submit', function (e) {
        e.preventDefault();

        const date = selectedDateInput.value;
        const empId = document.getElementById('employeeId').value;
        const firstName = document.getElementById('firstName').value;
        const lastName = document.getElementById('lastName').value;
        const section = document.getElementById('section').value;
        const quantity = document.getElementById('quantity').value;
        const time = document.getElementById('time').value;
        const menu = menuSelect.value;

        // Add reservation to calendar
        calendar.addEvent({
            title: `${firstName} ${lastName} (${menu} x${quantity})`,
            start: date + 'T' + time,
            allDay: false,
            extendedProps: {
                employeeId: empId,
                section: section,
                quantity: quantity,
                menu: menu
            }
        });

        //modal.hide();
        this.reset();
    });

});

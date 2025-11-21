document.addEventListener('DOMContentLoaded', function () {
    const calendarEl = document.getElementById('calendar');
    const calendar = new FullCalendar.Calendar(calendarEl, {
        initialView: 'dayGridMonth',
        eventDisplay: 'block',

        events: function (fetchInfo, successCallback, failureCallback) {
            fetch('../Order/GetOrdersForCalendar')
                .then(res => res.json())
                .then(data => successCallback(data))
                .catch(err => failureCallback(err));
        },

        dayCellDidMount: function (info) {
            const today = new Date();
            const cellDate = info.date;
            if (
                cellDate.getFullYear() === today.getFullYear() &&
                cellDate.getMonth() === today.getMonth() &&
                cellDate.getDate() === today.getDate()
            ) {
                info.el.style.backgroundColor = '#dbeafe';
                info.el.style.border = '2px solid #3b82f6';
            }
        }
    });


    calendar.render();
});



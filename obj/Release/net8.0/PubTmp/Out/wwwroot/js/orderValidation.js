document.addEventListener("DOMContentLoaded", function () {
    const form = document.getElementById("reservationForm");

    if (!form) {
        console.error("Reservation form not found!");
        return;
    }

    form.addEventListener("submit", function (e) {
        e.preventDefault();

        const formData = new FormData(form);

        fetch("../Order/PlaceOrder", {
            method: "POST",
            body: formData
        })
            .then(async response => {
                const contentType = response.headers.get("content-type");
                let result;

                if (contentType && contentType.includes("application/json")) {
                    result = await response.json();
                } else {
                    const text = await response.text();
                    throw new Error(`Unexpected response: ${text}`);
                }

                if (response.ok) {
                    Swal.fire({
                        icon: 'success',
                        title: 'Order Placed!',
                        html: `Your meal reservation has been recorded.<br/><strong>Reference #:</strong> ${result.referenceNumber || 'N/A'}`,
                    }).then(() => {
                        form.reset();
                        const modal = bootstrap.Modal.getInstance(document.getElementById("OrderModal"));
                        if (modal) modal.hide();
                    });
                } else {
                    Swal.fire({
                        icon: 'error',
                        title: 'Error',
                        text: result.message || 'Something went wrong.',
                    });
                }
            })
            .catch(error => {
                Swal.fire({
                    icon: 'error',
                    title: 'Error',
                    text: error.message,
                });
            });
    });
});

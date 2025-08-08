document.addEventListener("DOMContentLoaded", function () {
    const empInput = document.querySelector('input[name="EmployeeId"]');
    const firstNameInput = document.querySelector('input[name="FirstName"]');
    const lastNameInput = document.querySelector('input[name="LastName"]');
    const emailInput = document.querySelector('input[name="Email"]');
    const sectionInput = document.querySelector('input[name="Section"]');

    const positionInput = document.querySelector('input[name="Position"]');
    const adidInput = document.querySelector('input[name="ADID"]');

    if (!empInput) {
        console.error("EmployeeId input not found.");
        return;
    }

    empInput.addEventListener("change", function () {
        const empId = this.value.trim().toUpperCase();
        this.value = empId;

        if (!empId) return;

        const fetchUrl = `${window.appBaseUrl || '/'}Employee/GetEmployeeById?id=${encodeURIComponent(empId)}`;

        fetch(fetchUrl)
            .then(async response => {
                const contentType = response.headers.get("content-type") || "";
                if (!response.ok || !contentType.includes("application/json")) {
                    const errorHtml = await response.text();
                    console.error("Expected JSON, got:", errorHtml);
                    throw new Error("Server returned invalid response.");
                }
                return response.json();
            })
            .then(data => {
                if (!data || data.success === false) {
                    throw new Error(data.message || "Employee not found.");
                }

                firstNameInput.value = data.firstName || "";
                lastNameInput.value = data.lastName || "";
                emailInput.value = data.email || "";
                sectionInput.value = data.section || "";
                positionInput.value = data.position || "";
                adidInput.value = data.adid || "";
            })
            .catch(error => {
                alert("Error: " + (error.message || "Unable to retrieve employee details."));
                firstNameInput.value = "";
                lastNameInput.value = "";
                emailInput.value = "";
                sectionInput.value = "";
                console.error("Fetch error:", error);
            });
    });

    //Password Confirmation/Validation
    const form = document.querySelector("form");
    const password = document.getElementById("password");
    const confirmPassword = document.getElementById("confirmPassword");
    const confirmError = confirmPassword.nextElementSibling; // span.text-danger after confirmPassword

    form.addEventListener("submit", function (e) {
        if (password.value !== confirmPassword.value) {
            e.preventDefault(); // stop form submission
            confirmError.textContent = "Passwords do not match.";
            confirmPassword.classList.add("is-invalid");
        } else {
            confirmError.textContent = "";
            confirmPassword.classList.remove("is-invalid");
        }
    });

    //Clear error when user starts typing again
    confirmPassword.addEventListener("input", function () {
        if (password.value === confirmPassword.value) {
            confirmError.textContent = "";
            confirmPassword.classList.remove("is-invalid");
        }
    });
});

//Redirect to login after registration
function showSuccessModalAndRedirect() {
    var modalElement = document.getElementById('successModal');
    var modal = new bootstrap.Modal(modalElement);
    modal.show();

    setTimeout(function () {
        window.location.href = '../Home/Login'; 
    }, 4000);
}



$(document).ready(function () {
    $("#registerForm").submit(function (e) {
        e.preventDefault();

        $.ajax({
            type: "POST",
            url: "../Home/Register", 
            data: $(this).serialize(),
            success: function (response) {
                showSuccessModalAndRedirect();
            },
            error: function (xhr) {
                alert("Registration failed");
                console.error(xhr.responseText);
            }
        });

    });
});
// Clinic Appointment System JavaScript

// Auto-dismiss alerts after 5 seconds
setTimeout(function () {
    var alerts = document.querySelectorAll('.alert');
    alerts.forEach(function (alert) {
        alert.style.transition = 'all 0.5s ease';
        alert.style.opacity = '0';
        setTimeout(function () {
            if (alert.parentNode) {
                alert.parentNode.removeChild(alert);
            }
        }, 500);
    });
}, 5000);

// Enable tooltips everywhere
document.addEventListener('DOMContentLoaded', function () {
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // Enable popovers everywhere
    var popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
    var popoverList = popoverTriggerList.map(function (popoverTriggerEl) {
        return new bootstrap.Popover(popoverTriggerEl);
    });
});

// Form validation enhancements
document.addEventListener('DOMContentLoaded', function () {
    // Add loading state to buttons on form submission
    var forms = document.querySelectorAll('form');
    forms.forEach(function (form) {
        form.addEventListener('submit', function () {
            var submitBtn = form.querySelector('button[type="submit"]');
            if (submitBtn) {
                submitBtn.disabled = true;
                var spinner = document.createElement('span');
                spinner.className = 'spinner-border spinner-border-sm me-2';
                spinner.setAttribute('role', 'status');
                spinner.setAttribute('aria-hidden', 'true');
                submitBtn.prepend(spinner);
            }
        });
    });

    // Auto-format phone numbers
    var phoneInput = document.getElementById('PhoneNumber');
    if (phoneInput) {
        phoneInput.addEventListener('input', function () {
            var phone = this.value.replace(/\D/g, '');
            if (phone.length === 10) {
                phone = phone.replace(/(\d{3})(\d{3})(\d{4})/, '($1) $2-$3');
                this.value = phone;
            }
        });
    }
});

function checkAppointmentAvailability() {
    var doctorId = document.getElementById('DoctorId')?.value;
    var date = document.getElementById('AppointmentDate')?.value;

    if (doctorId && date) {
        var availabilityStatus = document.getElementById('availabilityStatus');
        if (availabilityStatus) {
            availabilityStatus.innerHTML = '<div class="spinner-border spinner-border-sm me-2" role="status"></div>Checking availability...';

            // Simulate API call - replace with actual API call
            setTimeout(function () {
                availabilityStatus.innerHTML = '<span class="text-success"><i class="fas fa-check-circle"></i> Time slots available</span>';
            }, 1000);
        }
    }
}

// Dashboard chart initialization (if using charts)
function initializeDashboardCharts() {
    // Example: Initialize charts if Chart.js is included
    if (typeof Chart !== 'undefined') {
        // Add chart initialization code here
        console.log('Charts can be initialized here');
    }
}

// Notification handling
function showNotification(message, type = 'info') {
    var alertClass = 'alert-' + type;
    var icon = type === 'success' ? 'fa-check-circle' :
        type === 'error' ? 'fa-exclamation-circle' :
            type === 'warning' ? 'fa-exclamation-triangle' : 'fa-info-circle';

    var notification = document.createElement('div');
    notification.className = 'alert ' + alertClass + ' alert-dismissible fade show';
    notification.setAttribute('role', 'alert');
    notification.innerHTML =
        '<i class="fas ' + icon + ' me-2"></i>' + message +
        '<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>';

    var main = document.querySelector('.container main');
    if (main) {
        main.prepend(notification);
    }

    // Auto-remove after 5 seconds
    setTimeout(function () {
        if (notification.parentNode) {
            var bsAlert = new bootstrap.Alert(notification);
            bsAlert.close();
        }
    }, 5000);
}

// Export functions for global use
window.ClinicAppointmentSystem = {
    showNotification: showNotification,
    initializeDashboardCharts: initializeDashboardCharts
};

// Initialize when document is ready
document.addEventListener('DOMContentLoaded', function () {
    initializeDashboardCharts();

    // Add event listeners for appointment availability
    var doctorSelect = document.getElementById('DoctorId');
    var dateInput = document.getElementById('AppointmentDate');

    if (doctorSelect && dateInput) {
        doctorSelect.addEventListener('change', checkAppointmentAvailability);
        dateInput.addEventListener('change', checkAppointmentAvailability);
    }
});
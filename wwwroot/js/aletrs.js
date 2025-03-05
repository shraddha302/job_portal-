document.addEventListener('DOMContentLoaded', function () {
    // Function to show alert
    function showAlert(message, type = 'success') {
        const alertDiv = document.createElement('div');
        alertDiv.className = `alert alert-${type}`;
        alertDiv.textContent = message;
        alertDiv.style.position = 'fixed';
        alertDiv.style.top = '20px';
        alertDiv.style.right = '20px';
        alertDiv.style.padding = '15px';
        alertDiv.style.zIndex = '1000';
        alertDiv.style.borderRadius = '5px';
        alertDiv.style.color = '#fff';
        alertDiv.style.backgroundColor = type === 'success' ? '#28a745' : '#dc3545';
        alertDiv.style.boxShadow = '0 4px 8px rgba(0, 0, 0, 0.1)';

        document.body.appendChild(alertDiv);

        setTimeout(() => {
            alertDiv.remove();
        }, 3000);
    }

    // Get TempData messages (populated from the server)
    const successMessage = '@TempData["Success"]';
    const errorMessage = '@TempData["Error"]';

    if (successMessage && successMessage !== 'null') {
        showAlert(successMessage, 'success');
    }

    if (errorMessage && errorMessage !== 'null') {
        showAlert(errorMessage, 'error');
    }
});

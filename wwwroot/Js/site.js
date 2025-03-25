window.downloadFileFromStream = async (fileName, contentStreamReference, contentType) => {
    const arrayBuffer = await contentStreamReference.arrayBuffer();
    const blob = new Blob([arrayBuffer], { type: contentType });

    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
};

window.ShowDeleteModal = () => {
    var modalEl = document.getElementById('deleteModal');
    if (modalEl) {
        var modal = new bootstrap.Modal(modalEl);
        modal.show();
    } else {
        console.error("Modal element not found!");
    }
};

window.HideDeleteModal = () => {
    var modalEl = document.getElementById('deleteModal');
    if (modalEl) {
        var modalInstance = bootstrap.Modal.getInstance(modalEl);
        if (modalInstance) {
            modalInstance.hide();
        } else {
            console.error("Modal instance not found!");
        }
    }
}; 
window.createBarChart = (canvasId, data) => {
    const ctx = document.getElementById(canvasId).getContext('2d');
    new Chart(ctx, {
        type: 'bar',
        data: {
            labels: data.map(d => d.label),
            datasets: [{
                label: 'Revenue',
                data: data.map(d => d.value),
                backgroundColor: 'rgba(54, 162, 235, 0.6)',
                borderColor: 'rgba(54, 162, 235, 1)',
                borderWidth: 1
            }]
        }
    });
};

window.createPieChart = (canvasId, data) => {
    const ctx = document.getElementById(canvasId).getContext('2d');
    new Chart(ctx, {
        type: 'pie',
        data: {
            labels: data.map(d => d.label),
            datasets: [{
                data: data.map(d => d.value),
                backgroundColor: ['#4CAF50', '#FF9800']
            }]
        }
    });
};


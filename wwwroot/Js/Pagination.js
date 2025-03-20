document.addEventListener("DOMContentLoaded", function () {
    const rowsPerPage = 3; // Set rows per page
    let currentPage = 1;

    function updateTableRows(tableId) {
        return document.querySelectorAll(`#${tableId} tbody tr`);
    }

    function displayPage(tableId, page) {
        let tableRows = updateTableRows(tableId);
        const start = (page - 1) * rowsPerPage;
        const end = start + rowsPerPage;
        tableRows.forEach((row, index) => {
            row.style.display = index >= start && index < end ? "" : "none";
        });
    }

    function setupPagination(tableId) {
        let tableRows = updateTableRows(tableId);
        const pageCount = Math.ceil(tableRows.length / rowsPerPage);
        const pagination = document.getElementById("pagination");
        pagination.innerHTML = "";

        if (pageCount <= 1) return;

        function createPageItem(text, page, disabled, active) {
            let li = document.createElement("li");
            li.className = `page-item ${disabled ? "disabled" : ""} ${active ? "active" : ""}`;
            let a = document.createElement("a");
            a.className = "page-link";
            a.href = "#";
            a.textContent = text;
            a.addEventListener("click", function (e) {
                e.preventDefault();
                if (text === "❮" && currentPage > 1) currentPage--;
                else if (text === "❯" && currentPage < pageCount) currentPage++;
                else if (!isNaN(text)) currentPage = parseInt(text);
                displayPage(tableId, currentPage);
                setupPagination(tableId);
            });
            li.appendChild(a);
            return li;
        }

        pagination.appendChild(createPageItem("❮", currentPage - 1, currentPage === 1));
        for (let i = 1; i <= pageCount; i++) {
            pagination.appendChild(createPageItem(i, i, false, i === currentPage));
        }
        pagination.appendChild(createPageItem("❯", currentPage + 1, currentPage === pageCount));
    }

    function updatePagination(tableId) {
        currentPage = 1;
        displayPage(tableId, currentPage);
        setupPagination(tableId);
    }

    document.querySelectorAll("#searchInput").forEach(input => {
        input.addEventListener("keyup", function () {
            var value = this.value.toLowerCase();
            var hasMatch = false;
            let tableId = "clientTable"; // Adjust if using multiple tables

            document.querySelectorAll(`#${tableId} tbody tr`).forEach(row => {
                var rowText = row.textContent.toLowerCase();
                var isMatch = rowText.indexOf(value) > -1;
                row.style.display = isMatch ? "" : "none";
                if (isMatch) hasMatch = true;
            });

            document.getElementById("noResults").classList.toggle("d-none", hasMatch);
            updatePagination(tableId);
        });
    });

    updatePagination("clientTable");
});

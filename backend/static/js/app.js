const API_URL = '/api/v1/donors';

document.addEventListener('DOMContentLoaded', () => {
    fetchDonors();
    setupEventListeners();
});

function setupEventListeners() {
    // Add Donor
    document.getElementById('addDonorForm').addEventListener('submit', async (e) => {
        e.preventDefault();
        const data = {
            name: document.getElementById('name').value,
            amount: parseInt(document.getElementById('amount').value),
            grade: document.getElementById('grade').value,
            message: document.getElementById('message').value
        };

        await apiRequest(API_URL, 'POST', data);
        e.target.reset();
        fetchDonors();
    });

    // Bulk Upload
    const uploadArea = document.getElementById('uploadArea');
    const fileInput = document.getElementById('csvFile');

    uploadArea.addEventListener('click', () => fileInput.click());

    uploadArea.addEventListener('dragover', (e) => {
        e.preventDefault();
        uploadArea.style.borderColor = '#3b82f6';
    });

    uploadArea.addEventListener('dragleave', () => {
        uploadArea.style.borderColor = '#e5e7eb';
    });

    uploadArea.addEventListener('drop', (e) => {
        e.preventDefault();
        uploadArea.style.borderColor = '#e5e7eb';
        handleFileUpload(e.dataTransfer.files[0]);
    });

    fileInput.addEventListener('change', (e) => {
        if (e.target.files.length > 0) handleFileUpload(e.target.files[0]);
    });

    // Refresh
    document.getElementById('refreshBtn').addEventListener('click', fetchDonors);

    // Search
    document.getElementById('searchInput').addEventListener('input', (e) => {
        const term = e.target.value.toLowerCase();
        const rows = document.querySelectorAll('#donorTable tbody tr');
        rows.forEach(row => {
            const name = row.children[1].textContent.toLowerCase();
            row.style.display = name.includes(term) ? '' : 'none';
        });
    });

    // Modal Close
    document.querySelector('.close').addEventListener('click', () => {
        document.getElementById('editModal').style.display = 'none';
    });

    // Edit Form Save
    document.getElementById('editDonorForm').addEventListener('submit', async (e) => {
        e.preventDefault();
        const id = document.getElementById('editId').value;
        const data = {
            name: document.getElementById('editName').value,
            amount: parseInt(document.getElementById('editAmount').value),
            grade: document.getElementById('editGrade').value,
            message: document.getElementById('editMessage').value
        };

        await apiRequest(`${API_URL}/${id}`, 'PUT', data);
        document.getElementById('editModal').style.display = 'none';
        fetchDonors();
    });
}

async function handleFileUpload(file) {
    if (!file) return;

    const formData = new FormData();
    formData.append('file', file);

    try {
        const response = await fetch(`${API_URL}/bulk`, {
            method: 'POST',
            body: formData
        });

        if (response.ok) {
            alert('Upload Successful!');
            fetchDonors();
        } else {
            const err = await response.json();
            alert(`Error: ${err.detail}`);
        }
    } catch (error) {
        console.error('Upload Error:', error);
        alert('Upload failed.');
    }
}

async function fetchDonors() {
    try {
        const response = await fetch(API_URL);
        const data = await response.json();
        renderTable(data);
    } catch (error) {
        console.error('Fetch Error:', error);
    }
}

function renderTable(donors) {
    const tbody = document.querySelector('#donorTable tbody');
    tbody.innerHTML = '';

    donors.forEach(donor => {
        const tr = document.createElement('tr');
        tr.innerHTML = `
            <td>${donor.id}</td>
            <td>${donor.name}</td>
            <td>${donor.amount.toLocaleString()}</td>
            <td><span class="badge ${donor.grade}">${donor.grade}</span></td>
            <td>${donor.message || '-'}</td>
            <td>
                <button class="btn btn-secondary btn-sm" onclick="openEditModal(${JSON.stringify(donor).replace(/"/g, '&quot;')})">Edit</button>
                <button class="btn btn-danger btn-sm" onclick="deleteDonor(${donor.id})">Delete</button>
            </td>
        `;
        tbody.appendChild(tr);
    });
}

async function deleteDonor(id) {
    if (!confirm('Are you sure you want to delete this contributor?')) return;

    await apiRequest(`${API_URL}/${id}`, 'DELETE');
    fetchDonors();
}

window.openEditModal = function (donor) {
    document.getElementById('editId').value = donor.id;
    document.getElementById('editName').value = donor.name;
    document.getElementById('editAmount').value = donor.amount;
    document.getElementById('editGrade').value = donor.grade;
    document.getElementById('editMessage').value = donor.message;

    document.getElementById('editModal').style.display = 'flex';
};

window.deleteDonor = deleteDonor;

async function apiRequest(url, method, data = null) {
    try {
        const options = {
            method: method,
            headers: {}
        };

        if (data) {
            options.headers['Content-Type'] = 'application/json';
            options.body = JSON.stringify(data);
        }

        const response = await fetch(url, options);
        if (!response.ok) {
            throw new Error('API Request Failed');
        }
        return await response.json();
    } catch (error) {
        console.error('API Error:', error);
        alert('Operation failed. Check console for details.');
    }
}

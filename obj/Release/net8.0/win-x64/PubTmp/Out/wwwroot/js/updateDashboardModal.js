document.addEventListener('DOMContentLoaded', function () {
    const itemModal = document.getElementById('itemModal');

    itemModal.addEventListener('show.bs.modal', function (event) {
        const button = event.relatedTarget;

        // Get data attributes from the clicked button
        const id = button.getAttribute('data-id');
        const name = button.getAttribute('data-name');
        const price = button.getAttribute('data-price');
        const description = button.getAttribute('data-description');
        const imagePath = button.getAttribute('data-image');

        // Populate modal fields
        document.getElementById('id').value = id;
        document.getElementById('name').value = name;
        document.getElementById('price').value = price;
        document.getElementById('description').value = description;


        const imagePreview = document.getElementById('imagePreview');
        if (imagePath && imagePath !== '') {
            imagePreview.src = imagePath;
            imagePreview.style.display = 'block';
        } else {
            imagePreview.style.display = 'none';
        }
    });
});
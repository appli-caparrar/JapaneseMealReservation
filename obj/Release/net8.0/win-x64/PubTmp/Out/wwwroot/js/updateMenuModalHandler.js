document.addEventListener('DOMContentLoaded', () => {
    const modal = document.getElementById('OrderModal');

    if (!modal) return; // Exit if modal doesn't exist

    modal.addEventListener('show.bs.modal', function (event) {
        const button = event.relatedTarget;

        // Use the actual data attributes your buttons have
        const name = button.getAttribute('data-menu-name') || '';
        const imageUrl = button.getAttribute('data-menu-image') || '';
        const availabilityDate = button.getAttribute('data-availability-date') || '';
        const TypeOfOrder = button.getAttribute('data-order-type') || '';


        // Set values to the correct input/image IDs in your modal
        modal.querySelector('#menuName').value = name;
        modal.querySelector('#orderTypeHidden').value = TypeOfOrder;
        modal.querySelector('#displayDate').value = availabilityDate;
        modal.querySelector('#menuImagePath').value = imageUrl;
        modal.querySelector('#hiddenDisplayDate').value = availabilityDate;
  
    
        // Update the image preview in the modal
        const imagePreview = modal.querySelector('#menuImage');
        if (imageUrl) {
            imagePreview.src = imageUrl;
            imagePreview.style.display = 'block';
        } else {
            imagePreview.style.display = 'none';
            imagePreview.src = '';
        }


    });

    
});


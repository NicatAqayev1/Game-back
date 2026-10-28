
// Copy Promo Code Functionality
function copyPromo(code) {
    const tempInput = document.createElement("input");
    tempInput.value = code;
    document.body.appendChild(tempInput);
    tempInput.select();
    document.execCommand("copy");
    document.body.removeChild(tempInput);

    const toast = document.getElementById("toast");
    const toastText = document.getElementById("toast-text");
    toastText.textContent = `Promokod "${code}" kopyalandı!`;

    toast.classList.add("show");

    setTimeout(() => {
        toast.classList.remove("show");
    }, 3000);
}

// Category Filter
let currentCategory = 'all';

function setCategory(btn, cat) {
    currentCategory = cat;
    document.querySelectorAll('.cat-btn').forEach(b => b.classList.remove('active'));
    btn.classList.add('active');
    filterOffers();
}

function filterOffers() {
    const searchVal = document.getElementById('search-input').value.toLowerCase();
    const cards = document.querySelectorAll('.offer-card');

    cards.forEach(card => {
        const categories = card.getAttribute('data-category');
        const textContent = card.textContent.toLowerCase();

        const matchesCategory = (currentCategory === 'all') || categories.includes(currentCategory);
        const matchesSearch = textContent.includes(searchVal);

        if (matchesCategory && matchesSearch) {
            card.style.display = 'flex';
        } else {
            card.style.display = 'none';
        }
    });
}

// FAQ Accordion Toggle
function toggleFaq(btn) {
    const item = btn.parentElement;
    item.classList.toggle('active');
}

// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Allow comma as decimal separator for number/range validation
(function ($) {
	if (!$.validator) return;

	var defaultNumber = $.validator.methods.number;
	var defaultRange = $.validator.methods.range;

	$.validator.methods.number = function (value, element) {
		if (value && typeof value === 'string') {
			value = value.replace(/\s/g, '').replace(',', '.');
		}
		return defaultNumber.call(this, value, element);
	};

	$.validator.methods.range = function (value, element, param) {
		if (value && typeof value === 'string') {
			value = value.replace(/\s/g, '').replace(',', '.');
		}
		return defaultRange.call(this, value, element, param);
	};
})(jQuery);

// Bootstrap confirm modal helper for delete forms
(function(){
	const modalEl = document.getElementById('confirmModal');
	if (!modalEl) return;
	const modal = new bootstrap.Modal(modalEl);
	const msgEl = document.getElementById('confirmModalMessage');
	const okBtn = document.getElementById('confirmModalOk');
	let pendingForm = null;

	document.addEventListener('submit', function(e){
		const form = e.target;
		if (form instanceof HTMLFormElement && form.dataset.confirm) {
			e.preventDefault();
			pendingForm = form;
			if (msgEl) msgEl.textContent = form.dataset.confirm;
			modal.show();
		}
	}, true);

	if (okBtn) {
		okBtn.addEventListener('click', function(){
			if (pendingForm) {
				const f = pendingForm;
				pendingForm = null;
				modal.hide();
				// submit after closing to avoid re-trigger
				setTimeout(()=>f.submit(), 0);
			}
		});
	}
})();

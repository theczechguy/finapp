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

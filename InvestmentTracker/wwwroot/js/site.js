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

// Loading States & Animations Management
(function() {
    'use strict';

    // Global loading state manager
    window.LoadingManager = {
        // Show full page loading overlay
        showOverlay: function(message = 'Loading...') {
            this.hideOverlay(); // Remove any existing overlay

            const overlay = document.createElement('div');
            overlay.className = 'loading-overlay';
            overlay.id = 'globalLoadingOverlay';

            // Check for dark mode
            if (document.body.classList.contains('dark-mode')) {
                overlay.classList.add('dark');
            }

            overlay.innerHTML = `
                <div class="text-center">
                    <div class="loading-spinner mb-3"></div>
                    <div class="text-muted">${message}</div>
                </div>
            `;

            document.body.appendChild(overlay);
            overlay.classList.add('fade-in');
        },

        // Hide loading overlay
        hideOverlay: function() {
            const overlay = document.getElementById('globalLoadingOverlay');
            if (overlay) {
                overlay.classList.add('fade-out');
                setTimeout(() => overlay.remove(), 300);
            }
        },

        // Show button loading state
        showButtonLoading: function(button, text = 'Loading...') {
            if (!button) return;

            button.classList.add('btn-loading');
            button.dataset.originalText = button.textContent;
            button.textContent = text;
            button.disabled = true;
        },

        // Hide button loading state
        hideButtonLoading: function(button) {
            if (!button) return;

            button.classList.remove('btn-loading');
            if (button.dataset.originalText) {
                button.textContent = button.dataset.originalText;
                delete button.dataset.originalText;
            }
            button.disabled = false;
        },

        // Create skeleton screen for table
        createTableSkeleton: function(rows = 5, columns = 5) {
            const skeleton = document.createElement('div');
            skeleton.className = 'table-skeleton';

            if (document.body.classList.contains('dark-mode')) {
                skeleton.classList.add('dark');
            }

            for (let i = 0; i < rows; i++) {
                const row = document.createElement('div');
                row.className = 'skeleton-row';

                for (let j = 0; j < columns; j++) {
                    const cell = document.createElement('div');
                    cell.className = `skeleton skeleton-cell`;
                    row.appendChild(cell);
                }

                skeleton.appendChild(row);
            }

            return skeleton;
        },

        // Show skeleton in container
        showSkeleton: function(container, skeletonType = 'table', options = {}) {
            if (!container) return;

            container.innerHTML = '';

            let skeleton;
            if (skeletonType === 'table') {
                skeleton = this.createTableSkeleton(options.rows || 5, options.columns || 5);
            } else if (skeletonType === 'card') {
                skeleton = this.createCardSkeleton();
            }

            if (skeleton) {
                container.appendChild(skeleton);
                container.classList.add('fade-in');
            }
        },

        // Create card skeleton
        createCardSkeleton: function() {
            const skeleton = document.createElement('div');
            skeleton.className = 'card';
            skeleton.innerHTML = `
                <div class="card-body">
                    <div class="skeleton skeleton-title mb-3"></div>
                    <div class="skeleton skeleton-text mb-2"></div>
                    <div class="skeleton skeleton-text mb-2" style="width: 80%;"></div>
                    <div class="skeleton skeleton-text mb-3" style="width: 60%;"></div>
                    <div class="d-flex gap-2">
                        <div class="skeleton skeleton-button"></div>
                        <div class="skeleton skeleton-button" style="width: 100px;"></div>
                    </div>
                </div>
            `;

            if (document.body.classList.contains('dark-mode')) {
                skeleton.classList.add('dark');
            }

            return skeleton;
        },

        // Hide skeleton and show content
        hideSkeleton: function(container, content) {
            if (!container) return;

            container.innerHTML = '';
            if (content) {
                container.appendChild(content);
            }
            container.classList.add('fade-in');
        },

        // Show progress indicator
        showProgress: function(container, progress = 0) {
            if (!container) return;

            let progressContainer = container.querySelector('.progress-container');
            if (!progressContainer) {
                progressContainer = document.createElement('div');
                progressContainer.className = 'progress-container';
                progressContainer.innerHTML = '<div class="progress-bar" style="width: 0%"></div>';
                container.appendChild(progressContainer);
            }

            const progressBar = progressContainer.querySelector('.progress-bar');
            if (progressBar) {
                progressBar.style.width = progress + '%';
            }
        },

        // Add bounce animation to element
        bounceIn: function(element) {
            if (!element) return;
            element.classList.add('bounce-in');
            setTimeout(() => element.classList.remove('bounce-in'), 600);
        },

        // Add slide up animation to element
        slideUp: function(element) {
            if (!element) return;
            element.classList.add('slide-up');
            setTimeout(() => element.classList.remove('slide-up'), 400);
        },

        // Add pulse animation to element
        pulse: function(element) {
            if (!element) return;
            element.classList.add('pulse');
            setTimeout(() => element.classList.remove('pulse'), 2000);
        }
    };

    // Auto-initialize loading states for forms
    document.addEventListener('DOMContentLoaded', function() {
        // Add loading states to all forms with data-loading attribute
        document.querySelectorAll('form[data-loading]').forEach(form => {
            form.addEventListener('submit', function(e) {
                const submitBtn = form.querySelector('button[type="submit"]');
                if (submitBtn) {
                    LoadingManager.showButtonLoading(submitBtn, form.dataset.loading || 'Saving...');
                }
            });
        });

        // Add loading states to buttons with data-loading attribute
        document.querySelectorAll('button[data-loading]').forEach(button => {
            button.addEventListener('click', function() {
                LoadingManager.showButtonLoading(button, button.dataset.loading);
            });
        });

        // Add page enter animation
        document.body.classList.add('page-enter');

        // Add fade-in animation to cards
        document.querySelectorAll('.card').forEach(card => {
            card.classList.add('fade-in');
        });

        // Add slide-up animation to alerts
        document.querySelectorAll('.alert').forEach(alert => {
            alert.classList.add('slide-up');
        });
    });

    // Handle AJAX loading states
    const originalFetch = window.fetch;
    window.fetch = function(...args) {
        // Show loading for API calls
        if (args[0] && typeof args[0] === 'string' && args[0].includes('/api/')) {
            LoadingManager.showOverlay('Loading data...');
            return originalFetch.apply(this, args).finally(() => {
                LoadingManager.hideOverlay();
            });
        }
        return originalFetch.apply(this, args);
    };

})();

// Toast notification system
(function() {
    window.Toast = {
        show: function(message, type = 'info', duration = 3000) {
            const toast = document.createElement('div');
            toast.className = `toast align-items-center text-white bg-${type} border-0`;
            toast.setAttribute('role', 'alert');
            toast.innerHTML = `
                <div class="d-flex">
                    <div class="toast-body">${message}</div>
                    <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
                </div>
            `;

            // Add to toast container
            let container = document.querySelector('.toast-container');
            if (!container) {
                container = document.createElement('div');
                container.className = 'toast-container position-fixed top-0 end-0 p-3';
                container.style.zIndex = '9999';
                document.body.appendChild(container);
            }

            container.appendChild(toast);

            // Initialize and show
            const bsToast = new bootstrap.Toast(toast, { autohide: true, delay: duration });
            bsToast.show();

            // Remove after hiding
            toast.addEventListener('hidden.bs.toast', () => {
                toast.remove();
            });
        },

        success: function(message, duration) {
            this.show(message, 'success', duration);
        },

        error: function(message, duration) {
            this.show(message, 'danger', duration);
        },

        warning: function(message, duration) {
            this.show(message, 'warning', duration);
        },

        info: function(message, duration) {
            this.show(message, 'info', duration);
        }
    };
})();

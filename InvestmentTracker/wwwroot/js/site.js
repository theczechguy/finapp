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

// Keyboard Shortcuts Manager
class KeyboardShortcutsManager {
    constructor() {
        this.shortcuts = new Map();
        this.isHelpVisible = false;
        this.init();
    }

    init() {
        document.addEventListener('keydown', (e) => this.handleKeyPress(e));
        this.registerShortcuts();
        this.showHint();
        this.initFormEnterHandling();
        this.initEscapeKeyHandling();  // Add direct ESC key handling
        
        // Make hide function globally available for HTML onclick
        window.hideKeyboardShortcuts = () => this.hideHelp();
    }

    registerShortcuts() {
        // Navigation shortcuts (global)
        this.shortcuts.set('h', () => this.navigateTo('/'));
        this.shortcuts.set('e', () => this.navigateTo('/Expenses/Index'));
        this.shortcuts.set('i', () => this.navigateTo('/Investments/List'));
        this.shortcuts.set('p', () => this.navigateTo('/Portfolio/Index'));
        this.shortcuts.set('v', () => this.navigateTo('/Values/Index'));

        // Help shortcut
        this.shortcuts.set('?', () => this.toggleHelp());

        // Escape to close modals/help
        this.shortcuts.set('Escape', () => this.handleEscape());

        // Register page-specific shortcuts
        this.registerPageSpecificShortcuts();
    }

    registerPageSpecificShortcuts() {
        const currentPath = window.location.pathname;

        // Expense page shortcuts
        if (currentPath.includes('/Expenses/Index') || currentPath === '/Expenses') {
            this.shortcuts.set('a', () => this.openModal('addRegularExpenseModal'));
            this.shortcuts.set('q', () => this.openModal('addIrregularExpenseModal'));
            this.shortcuts.set('o', () => this.openModal('addOneTimeIncomeModal'));
            this.shortcuts.set('[', () => this.navigateMonth(-1));
            this.shortcuts.set(']', () => this.navigateMonth(1));
            this.shortcuts.set('u', () => this.focusFirstIncomeField());
        }

        // Investment pages shortcuts
        if (currentPath.includes('/Investments')) {
            this.shortcuts.set('n', () => this.navigateTo('/Investments/Create'));
        }

        // Values page shortcuts
        if (currentPath.includes('/Values') || (currentPath.includes('/Investments/') && currentPath.includes('Values'))) {
            this.shortcuts.set('a', () => this.focusValueForm());
            this.shortcuts.set('c', () => this.focusContributionForm());
            this.shortcuts.set('f', () => this.focusSearchField());
        }
    }

    handleKeyPress(e) {
        // Don't trigger shortcuts when user is typing in input fields
        if (this.isInputField(e.target)) {
            // Allow Escape and ? even in input fields
            if (e.key !== 'Escape' && e.key !== '?') {
                return;
            }
        }

        const key = e.key.toLowerCase();
        const shortcut = this.shortcuts.get(key);

        if (shortcut) {
            e.preventDefault();
            shortcut();
        } else if (key === 'escape') {
            e.preventDefault();
            this.handleEscape();
        }
    }

    isInputField(element) {
        return element.tagName === 'INPUT' ||
               element.tagName === 'TEXTAREA' ||
               element.tagName === 'SELECT' ||
               element.contentEditable === 'true';
    }

    navigateTo(path) {
        window.location.href = path;
    }

    toggleHelp() {
        if (this.isHelpVisible) {
            this.hideHelp();
        } else {
            this.showHelp();
        }
    }

    showHelp() {
        const overlay = document.getElementById('keyboardShortcutsOverlay');
        if (overlay) {
            overlay.style.display = 'flex';
            overlay.tabIndex = -1; // Make it focusable
            overlay.focus(); // Set focus to the overlay
            this.isHelpVisible = true;
            
            // Add ESC key listener to the overlay
            this.overlayKeydownHandler = (e) => {
                if (e.key === 'Escape') {
                    this.hideHelp();
                }
            };
            overlay.addEventListener('keydown', this.overlayKeydownHandler);
        }
    }

    hideHelp() {
        const overlay = document.getElementById('keyboardShortcutsOverlay');
        if (overlay) {
            overlay.style.display = 'none';
            this.isHelpVisible = false;
            
            // Remove the keydown listener
            if (this.overlayKeydownHandler) {
                overlay.removeEventListener('keydown', this.overlayKeydownHandler);
                this.overlayKeydownHandler = null;
            }
        }
    }

    handleEscape() {
        // Close help overlay if visible
        if (this.isHelpVisible) {
            this.hideHelp();
            return;
        }

        // Close any open Bootstrap modals
        const openModals = document.querySelectorAll('.modal.show');
        if (openModals.length > 0) {
            const lastModal = openModals[openModals.length - 1];
            const modalInstance = bootstrap.Modal.getInstance(lastModal);
            if (modalInstance) {
                modalInstance.hide();
            }
            return;
        }

        // Handle ESC on form pages as cancel/back action
        this.handleFormCancel();
    }

    handleFormCancel() {
        const currentPath = window.location.pathname;

        // First, try to find any cancel/back button on the current page
        const cancelSelectors = [
            'a.btn-secondary[href*="List"]',  // Cancel links to List
            'a.btn-secondary',                // Any secondary button link
            'button.btn-secondary',           // Secondary buttons
            'a[title*="cancel" i]',          // Links with cancel in title
            'a[title*="back" i]',            // Links with back in title
            '.cancel-btn',                    // Cancel class
            '.back-btn'                       // Back class
        ];

        for (const selector of cancelSelectors) {
            const buttons = document.querySelectorAll(selector);
            for (const btn of buttons) {
                // Skip buttons that are for deleting or other destructive actions
                const text = btn.textContent.toLowerCase();
                const title = (btn.title || '').toLowerCase();
                const href = (btn.href || '').toLowerCase();

                if (!text.includes('delete') && !text.includes('remove') &&
                    !title.includes('delete') && !title.includes('remove') &&
                    !href.includes('delete') && !href.includes('remove')) {
                    btn.click();
                    return;
                }
            }
        }

        // Specific handling for investment pages
        if (currentPath.includes('/Investments/Create') || currentPath.includes('/Investments/Edit')) {
            this.navigateTo('/Investments/List');
            return;
        }

        // Values page - go back to investment list
        if (currentPath.includes('/Values')) {
            this.navigateTo('/Investments/List');
            return;
        }

        // If no specific cancel action found, navigate back in history
        if (window.history.length > 1) {
            window.history.back();
        }
    }

    showHint() {
        // Show a subtle hint about keyboard shortcuts after a delay
        setTimeout(() => {
            if (!localStorage.getItem('keyboardHintsDismissed')) {
                this.createHint();
            }
        }, 3000);
    }

    createHint() {
        const hint = document.createElement('div');
        hint.className = 'keyboard-hint';
        hint.innerHTML = '💡 Press <kbd>?</kbd> for keyboard shortcuts';
        hint.onclick = () => {
            this.showHelp();
            hint.remove();
        };

        // Auto-remove after 10 seconds
        setTimeout(() => {
            if (hint.parentNode) {
                hint.remove();
            }
        }, 10000);

        document.body.appendChild(hint);
    }

    openModal(modalId) {
        const modal = document.getElementById(modalId);
        if (modal) {
            const modalInstance = new bootstrap.Modal(modal);
            modalInstance.show();
            
            // Focus the first input field in the modal after it's shown
            modal.addEventListener('shown.bs.modal', () => {
                const firstInput = modal.querySelector('input, select, textarea');
                if (firstInput) {
                    firstInput.focus();
                }
                
                // Add Enter key handler to submit the form from any field
                const form = modal.querySelector('form');
                if (form) {
                    this.modalEnterHandler = (e) => {
                        if (e.key === 'Enter' && !e.shiftKey) {
                            e.preventDefault();
                            form.requestSubmit();
                        }
                    };
                    modal.addEventListener('keydown', this.modalEnterHandler);
                }
            }, { once: true });
            
            // Remove the handler when modal is hidden
            modal.addEventListener('hidden.bs.modal', () => {
                if (this.modalEnterHandler) {
                    modal.removeEventListener('keydown', this.modalEnterHandler);
                    this.modalEnterHandler = null;
                }
            }, { once: true });
        }
    }

    navigateMonth(direction) {
        if (typeof window.navigateMonth === 'function') {
            window.navigateMonth(direction);
        }
    }

    focusFirstIncomeField() {
        // Find the first income update input field
        const firstIncomeInput = document.querySelector('input[name="actualAmount"]');
        if (firstIncomeInput) {
            firstIncomeInput.focus();
            firstIncomeInput.select();
        }
    }

    focusSearchField() {
        // Focus the first search/filter input
        const searchInput = document.querySelector('input[type="search"], input[name*="search"], #Provider, #InvestmentId');
        if (searchInput) {
            searchInput.focus();
        }
    }

    focusValueForm() {
        // Focus the first input in the add value form
        // Try multiple selectors to find the form
        let valueForm = document.querySelector('form[asp-page-handler="AddValue"]');
        if (!valueForm) {
            // Fallback: find form that contains value input
            valueForm = document.querySelector('input[name*="Value"][type="text"]').closest('form');
        }
        if (!valueForm) {
            // Another fallback: find form with "Add Value" heading above it
            const heading = Array.from(document.querySelectorAll('h5')).find(h => h.textContent.includes('Add Value'));
            if (heading) {
                valueForm = heading.nextElementSibling;
                while (valueForm && valueForm.tagName !== 'FORM') {
                    valueForm = valueForm.nextElementSibling;
                }
            }
        }

        if (valueForm) {
            const firstInput = valueForm.querySelector('input[type="date"], input[type="text"]');
            if (firstInput) {
                firstInput.focus();
                firstInput.select();
            }
        }
    }

    focusContributionForm() {
        // Focus the first input in the add contribution form
        // Try multiple selectors to find the form
        let contributionForm = document.querySelector('form[asp-page-handler="AddContribution"]');
        if (!contributionForm) {
            // Fallback: find form that contains contribution input
            contributionForm = document.querySelector('input[name*="Contribution"][type="text"]').closest('form');
        }
        if (!contributionForm) {
            // Another fallback: find form with "One-time Contributions" heading above it
            const heading = Array.from(document.querySelectorAll('h5')).find(h => h.textContent.includes('One-time Contributions'));
            if (heading) {
                contributionForm = heading.nextElementSibling;
                while (contributionForm && contributionForm.tagName !== 'FORM') {
                    contributionForm = contributionForm.nextElementSibling;
                }
            }
        }

        if (contributionForm) {
            const firstInput = contributionForm.querySelector('input[type="date"], input[type="text"]');
            if (firstInput) {
                firstInput.focus();
                firstInput.select();
            }
        }
    }

    initEscapeKeyHandling() {
        // Direct ESC key handler as backup to main keyboard shortcut system
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape' || e.key === 'Esc') {
                // Don't interfere if user is in an input field and it's not for navigation
                const activeElement = document.activeElement;
                if (activeElement && (activeElement.tagName === 'INPUT' || activeElement.tagName === 'TEXTAREA' || activeElement.tagName === 'SELECT')) {
                    // Allow ESC in form fields for navigation/cancellation
                }

                // Check if help overlay is visible
                if (this.isHelpVisible) {
                    this.hideHelp();
                    e.preventDefault();
                    return;
                }

                // Check for open modals
                const openModals = document.querySelectorAll('.modal.show');
                if (openModals.length > 0) {
                    const lastModal = openModals[openModals.length - 1];
                    const modalInstance = bootstrap.Modal.getInstance(lastModal);
                    if (modalInstance) {
                        modalInstance.hide();
                        e.preventDefault();
                        return;
                    }
                }

                // Handle form cancellation
                this.handleFormCancel();
                e.preventDefault();
            }
        }, true); // Use capture phase to ensure we get the event first
    }
}

new KeyboardShortcutsManager();

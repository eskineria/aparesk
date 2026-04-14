import { toast } from 'react-toastify';
import i18n from '@/i18n';

export const showToast = (message: string, type: 'success' | 'error' | 'warning' | 'info' = 'success') => {
    toast(message, { type });
}

export const showValidationToast = (errors: unknown) => {
    let message = '';

    if (typeof errors === 'string') {
        message = errors;
    } else if (Array.isArray(errors)) {
        message = errors.join('\n'); // Use newline instead of <br/>
    } else if (typeof errors === 'object' && errors !== null) {
        // Handle ASP.NET Core ProblemDetails format
        const flatErrors = Object.values(errors).flat();
        if (flatErrors.length > 0) {
            message = flatErrors.join('\n');
        }
    }

    // Default message if parsing failed or was empty
    if (!message) {
        message = i18n.t('auth.errors.default');
    }

    // Display toast with whitespace-pre-line to respect newlines
    toast.error(message, {
        style: { whiteSpace: 'pre-line' }
    });
}

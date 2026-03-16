import { sharedTranslations } from "./sharedTranslations";

export function localizedBootboxConfirm(options: BootboxConfirmOptions) {
    options.buttons = {
        confirm: {
            label: sharedTranslations.ok
        },
        cancel: {
            label: sharedTranslations.cancel
        }
    };

    bootbox.confirm(options);
}

export function localizedBootboxAlert(options: BootboxAlertOptions) {
    options.buttons = {
        ok: {
            label: sharedTranslations.ok
        }
    };

    bootbox.alert(options);
}
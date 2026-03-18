import { frontendSettings } from "./frontendSettingsHelper";

export function localizedBootboxConfirm(options: BootboxConfirmOptions) {
    options.buttons = {
        confirm: {
            label: frontendSettings.sharedTranslations.ok
        },
        cancel: {
            label: frontendSettings.sharedTranslations.cancel
        }
    };

    bootbox.confirm(options);
}

export function localizedBootboxAlert(options: BootboxAlertOptions) {
    options.buttons = {
        ok: {
            label: frontendSettings.sharedTranslations.ok
        }
    };

    bootbox.alert(options);
}
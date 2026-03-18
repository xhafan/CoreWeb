import { FrontendSettings } from "./Generated/FrontendSettings";
import { FrontendSettingsConstants } from "./Generated/FrontendSettingsConstants";

function getFrontendSettings(): FrontendSettings {
    const frontendSettingsScriptElement = document.getElementById(FrontendSettingsConstants.frontendSettingsScriptId);
    if (!frontendSettingsScriptElement?.textContent) throw new Error(`${FrontendSettingsConstants.frontendSettingsScriptId} script not found.`);
    return JSON.parse(frontendSettingsScriptElement.textContent) as FrontendSettings;
}

export const frontendSettings = getFrontendSettings();

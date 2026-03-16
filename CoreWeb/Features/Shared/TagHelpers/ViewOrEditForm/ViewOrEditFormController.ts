import { JobDto } from "../../Generated/JobDto";
import { DomainConstants } from "../../Generated/DomainConstants";
import BaseStimulusModelController from "../../BaseStimulusModelController";
import { formSubmitWithWaitForJobCompletion } from "../../../Jobs/jobCompletion";
import { ViewOrEditFormStimulusModel } from "../../Generated/ViewOrEditFormStimulusModel";

export const formSubmitJobCompletedEventName = "formSubmitJobCompleted";
export const formEditingCancelledEventName = "formEditingCancelled";

export default class ViewOrEditFormController extends BaseStimulusModelController<ViewOrEditFormStimulusModel> {
    static targets  = [
        "form",
        "viewModeItem",
        "editModeItem",
        "edit",
        "save",
        "cancel"
    ];

    declare formTarget: HTMLFormElement;
    declare viewModeItemTargets: HTMLElement[];
    declare editModeItemTargets: HTMLElement[];
    declare editTarget: HTMLElement;
    declare saveTarget: HTMLElement;
    declare cancelTarget: HTMLElement;

    connect() {
        const form = this.formTarget;

        formSubmitWithWaitForJobCompletion(
            form, 
            jobDto => {
                this.raiseEventFormSubmitJobCompleted(jobDto);
            }
        );

        if (this.modelValue.startInEditMode) {
            this.edit();
        }
    }

    private raiseEventFormSubmitJobCompleted(job: JobDto) {
        this.formTarget.dispatchEvent(new CustomEvent(formSubmitJobCompletedEventName, { bubbles: true, detail: job }));
    }

    edit() {
        this.viewModeItemTargets.forEach(item => {
            item.style.display = "none";
        });
        this.editModeItemTargets.forEach(item => {
            item.style.display = "";
        });

        this.editTarget.style.display = "none"; 
        this.saveTarget.style.display = "";
        if (!this.modelValue.hideCancelInEditMode) {
            this.cancelTarget.style.display = "";
        }
    }

    cancel() {
        this.raiseEventFormEditingCancelled();
    }

    private raiseEventFormEditingCancelled() {
        this.formTarget.dispatchEvent(new Event(formEditingCancelledEventName, {bubbles: true}));
    }
}
import Enumerable from "linq";
import { DomainConstants } from "../Shared/Generated/DomainConstants";
import { JobUrlConstants } from "../Shared/Generated/JobUrlConstants";
import { JobDto } from "../Shared/Generated/JobDto";
import { RebusConstants } from "../Shared/Generated/RebusConstants";
import { logError } from "../Shared/logging";
import { sharedTranslations } from "../Shared/sharedTranslations";
import { localizedBootboxConfirm } from "../Shared/bootboxHelper";

export function formSubmitWithWaitForJobCompletion(
    form: HTMLFormElement,
    onJobCompletion: (job: JobDto) => void,    
    confirmationMessage: string | undefined = undefined,
    confirmationTitle: string | undefined = undefined
) {
    form.addEventListener("submit", async (event: SubmitEvent) => {
        event.preventDefault();

        if (!$(form).valid()) {
            return false;
        }

        if (confirmationMessage) {
            if (!confirmationTitle) {
                localizedBootboxConfirm({
                    message: confirmationMessage,
                    callback: async result => {
                        if (!result) return;

                        await sendRequestAndWaitForJobCompletionCommon();
                    }
                });
            } else {
                localizedBootboxConfirm({                                      
                    title: confirmationTitle,
                    message: confirmationMessage,
                    callback: async result => {
                        if (!result) return;

                        await sendRequestAndWaitForJobCompletionCommon();
                    }
                });
            }
        }
        else {
            await sendRequestAndWaitForJobCompletionCommon();
        }
            
        async function sendRequestAndWaitForJobCompletionCommon() {
            sendRequestAndWaitForJobCompletion(
                form, 
                event.submitter,
                onJobCompletion
            );
        }
    });
}

async function sendRequestAndWaitForJobCompletion(
    form: HTMLFormElement,
    submitter: HTMLElement | null = null,
    onJobCompletion: (job: JobDto) => void,
) {       
    disableElementAndAddSpinner(submitter);

    try {

        const response = await fetch(form.action, {
            method: form.method,
            body: new FormData(form)
        });

        if (response.status === 422) {
            refreshFormValidationErrorsFromReponse();            
            enableElementAndRemoveSpinner(submitter);
            return; 

            async function refreshFormValidationErrorsFromReponse() {
                const html = await response.text();

                const parser = new DOMParser();
                const doc = parser.parseFromString(html, "text/html");

                const newValidationSpans = Enumerable.from(doc.querySelectorAll("span[data-valmsg-for]"));

                form.querySelectorAll("span[data-valmsg-for]").forEach(currentSpan => {
                    const validationMessageFor = currentSpan.getAttribute("data-valmsg-for");
                    if (!validationMessageFor) return;

                    const newValidationSpan = newValidationSpans.singleOrDefault(x => x.getAttribute("data-valmsg-for") === validationMessageFor);

                    if (newValidationSpan) {
                        currentSpan.className = newValidationSpan.className;
                        currentSpan.innerHTML = newValidationSpan.innerHTML;
                    }
                });

            }
        }

        if (!response.ok) {
            throw new Error(`Failed to submit form: HTTP ${response.status}`);
        }

        let jobGuid = await response.text()
        if (!jobGuid) {
            throw new Error("Error getting job Guid.");
        }

        const jobDto = await waitForJobCompletion(jobGuid);
       
        onJobCompletion(jobDto);
    }
    catch (error) {
        await logError(error, {}, true, true);
    }
       
    enableElementAndRemoveSpinner(submitter);
}

export async function waitForJobCompletion(jobGuid: string) : Promise<JobDto> {
    let getJobUrl = JobUrlConstants.getJobUrlTemplate.replace(JobUrlConstants.jobGuidVariable, jobGuid);

    const pollJob = async (delay = 300): Promise<JobDto> => {
        await new Promise(resolve => setTimeout(resolve, delay));
        const response = await fetch(getJobUrl);
        if (response.ok) {
            const jobDto = await response.json() as JobDto;
            if (jobDto.completedOn) {
                return jobDto;
            }
            else if (jobDto.numberOfHandlingAttempts >= RebusConstants.maxDeliveryAttempts 
                     || (getJobLastException(jobDto)?.includes(RebusConstants.rebusMessageCouldNotBeDispatchedToAnyHandlersException))) {
                throw new Error(`Job ${jobDto.guid} failed.`);
            }
        }
        return pollJob(Math.min(delay * 2, 8000));
    };

    const jobDto = await pollJob();
    
    return jobDto;
}

function getJobLastException(jobDto: JobDto) : string | undefined {
    let lastJobHandlingAttempt = Enumerable.from(jobDto.handlingAttempts).orderByDescending(x => x.endedOn).firstOrDefault();
    return lastJobHandlingAttempt?.exception;
}

export async function getRelatedDomainEventJobGuid(commandJobGuid: string, domainEventType: string): Promise<string | null> {
    let getRelatedDomainEventJobUrl = JobUrlConstants.getRelatedDomainEventJobUrlTemplate
        .replace(JobUrlConstants.commandJobGuidVariable, commandJobGuid)
        .replace(JobUrlConstants.domainEventTypeVariable, domainEventType);

    const response = await fetch(getRelatedDomainEventJobUrl);
    if (response.ok) {
        const jobDto = await response.json() as JobDto;
        return jobDto.guid;
    }

    return null;
}

export function disableElementAndAddSpinner(element: HTMLElement | null) {
    if (!element) return;

    element.setAttribute("disabled", "disabled");

    if (!element.querySelector("span[class^='spinner-border']")) {
        const spinner = document.createElement("span");
        spinner.className = "spinner-border spinner-border-sm me-2";
        element.prepend(spinner);
    }
}

function enableElementAndRemoveSpinner(element: HTMLElement | null) {
    if (!element) return;
    
    element.removeAttribute("disabled");
    element.querySelectorAll("span[class^='spinner-border']").forEach(spinnerSpan => spinnerSpan.remove());    
}
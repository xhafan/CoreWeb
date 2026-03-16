export const registerGlobalEventHandlerEventName = "registerGlobalEventHandler";

export class EventHandlerRegistration<T> {
    eventName: string;
    eventHandler: (event: CustomEventInit<T>) => void;

    constructor(eventName: string, eventHandler: (event: CustomEventInit<T>) => void) {
        this.eventName = eventName;
        this.eventHandler = eventHandler;
    }
}

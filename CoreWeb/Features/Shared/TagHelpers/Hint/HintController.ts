import { Controller } from "@hotwired/stimulus";

export default class HintController extends Controller {
    static targets  = [
        "hintLink"
    ];

    declare hintLinkTarget: HTMLElement;

    connect() {
        // @ts-ignore
        new bootstrap.Popover(this.hintLinkTarget);
    }
}
import { Controller } from "@hotwired/stimulus";

export default class BaseStimulusModelController<TModel> extends Controller {
    static values = {
        model: Object
    }

    declare modelValue: TModel;
}
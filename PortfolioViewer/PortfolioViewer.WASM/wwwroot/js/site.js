"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
function scrollToBottom(id) {
    const element = document.getElementById(id);
    if (element) {
        element.scrollTop = element.scrollHeight;
    }
}

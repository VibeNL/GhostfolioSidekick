function scrollToBottom(id: string): void {
    const element = document.getElementById(id);
    if (element) {
        element.scrollTop = element.scrollHeight;
    }
}

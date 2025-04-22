let audio = null;

window.playAudio = (audioUrl, startPosition) => {
    if (!audio || audio.src !== audioUrl) {
        audio = new Audio(audioUrl);
    }
    audio.currentTime = startPosition;
    audio.play().catch(error => console.error("Audio play error:", error));
};

window.pauseAudio = () => {
    if (audio && !audio.paused) {
        audio.pause();
        return audio.currentTime;
    }
    return 0;
};
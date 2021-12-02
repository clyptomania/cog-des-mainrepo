
public struct EmotPlaylistElement {

    public EmotPlaylistElement(string roomName, int duration, int trial_idx, string instruction, string lab) {
        this.roomName = roomName;
        this.duration = duration;
        this.trial_idx = trial_idx;
        this.instruction = instruction;
        this.lab = lab;
    }

    public readonly string roomName;
    public readonly int duration;
    public readonly int trial_idx;
    public readonly string instruction;
    public readonly string lab;

    public string expName => $"{lab}_{trial_idx}_{roomName}_{instruction}_{duration}";
}

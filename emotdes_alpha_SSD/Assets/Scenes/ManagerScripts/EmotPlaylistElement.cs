
public struct EmotPlaylistElement {

    public EmotPlaylistElement(string roomName, int duration, int task_idx, int order_idx) {
        this.roomName = roomName;
        this.duration = duration;
        this.task_idx = task_idx;
        this.order_idx = order_idx;
    }

    public readonly string roomName;
    public readonly int duration;
    public readonly int task_idx;
    public readonly int order_idx;

    public string expName => $"{roomName}_{duration}_{task_idx}_{order_idx}";
}

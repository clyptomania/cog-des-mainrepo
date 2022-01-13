public struct EmotPlaylistElement {

    public EmotPlaylistElement (string roomName, int duration, int trial_idx, ExpeControl.instruction instruction, string lab, int participant) {
        this.roomName = roomName;
        this.duration = duration;
        this.trial_idx = trial_idx;
        this.instruction = instruction;
        this.lab = lab;
        this.participant = participant;
        // this.varOne = varOne;
        // this.varTwo = varTwo;
    }

    public readonly string roomName;
    public readonly int duration;
    public readonly int trial_idx;
    // public readonly string instruction;
    public readonly ExpeControl.instruction instruction;
    public readonly string lab;
    public readonly int participant;
    // public readonly string varOne;
    // public readonly string varTwo;

    public string expNameCSV => $"{lab},{participant+1},{trial_idx},{roomName},{instruction},{duration}";
    // public string expName => $"{lab}_{participant+1}_{trial_idx+1}_{roomName}_{instruction}_{duration}";
    public string expName {
        get {
            string condensedRoomName = "";
            string[] roomNameParts = roomName.Split (' ');
            if (roomNameParts.Length > 1) {
                foreach (string part in roomNameParts) {
                    if (part == " " || part == "_" || part == "-" || part == "?")
                        continue;
                    else
                        condensedRoomName += part;
                }
            } else {
                condensedRoomName = roomName;
            }
            return $"{lab}_{participant+1}_{trial_idx}_{condensedRoomName}_{instruction}_{duration}";
        }
    }
}
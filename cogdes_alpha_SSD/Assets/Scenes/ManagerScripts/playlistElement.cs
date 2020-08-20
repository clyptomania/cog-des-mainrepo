using UnityEngine;

public struct playlistElement
{
    public playlistElement(int room_idx, int task_idx, int order_idx)
    {
        this.room_idx = room_idx;
        this.task_idx = task_idx;
        this.order_idx = order_idx;
    }

    public readonly int room_idx;
    public readonly int task_idx;
    public readonly int order_idx;

    public string room_name => RoomManager.RoomNames[room_idx].Split(' ')[1];

    public string expName => $"{room_name}_{task_idx}_{order_idx}";
}
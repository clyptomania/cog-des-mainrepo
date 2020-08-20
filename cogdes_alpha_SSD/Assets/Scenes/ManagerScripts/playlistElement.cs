using UnityEngine;

public struct playlistElement
{
    public playlistElement(int room_idx, int task_idx, int order_idx)
    {
        this.room_idx = room_idx;
        this.exp_idx = task_idx;
        this.order_idx = order_idx;
    }

    public readonly int room_idx;
    public readonly int exp_idx;
    public readonly int order_idx;

    public string room_name => RoomManager.RoomNames[room_idx];

    public string expName => $"{room_name}_{exp_idx}_{order_idx}";

}
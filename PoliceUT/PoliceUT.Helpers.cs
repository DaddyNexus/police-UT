using System;
using Newtonsoft.Json;
using UnityEngine;

namespace nexusUT
{
    #region Helper Classes
    public class JailCell
    {
        public string Name { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Radius { get; set; }

        [JsonIgnore]
        public Vector3 Position => new Vector3(X, Y, Z);
    }

    public class JailedPlayerData
    {
        public Coroutine ReleaseCoroutine { get; set; }
        public JailCell AssignedCell { get; set; }
        public DateTime ReleaseUtcTime { get; set; }
        public uint BailAmount { get; set; }
    }

    public class DragState
    {
        public bool IsInVehicle { get; set; } = false;
    }

    public class PersistentJailInfo
    {
        public ulong PlayerId { get; set; }
        public string CellName { get; set; }
        public double SecondsRemaining { get; set; }
        public uint BailAmount { get; set; }
        public string Reason { get; set; }
    }
    #endregion
}

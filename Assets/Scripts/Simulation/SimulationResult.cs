using System.Collections.Generic;

namespace BrokenAnchor.Simulation
{
    public class SimulationResult
    {
        public bool success;
        public bool narrowSuccess;
        public bool shipEnteredDangerZone;
        public float remainingDistance;
        public float anchorDamage;
        public readonly List<string> reasons = new List<string>();
    }
}

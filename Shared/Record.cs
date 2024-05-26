using CitizenFX.Core;

namespace RecM
{
    public class Record
    {
        public int Time { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Right { get; set; }
        public Vector3 Forward { get; set; }
        public Vector3 Velocity { get; set; }
        public float SteeringAngle { get; set; }
        public float Gas { get; set; }
        public float Brake { get; set; }
        public bool UseHandbrake { get; set; }
    }
}

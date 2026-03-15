namespace RecM
{
    public class PIDController
    {
        private readonly float _kp;
        private readonly float _ki;
        private readonly float _kd;
        private float _previousError;
        private float _integral;

        public PIDController(float kp, float ki, float kd)
        {
            _kp = kp;
            _ki = ki;
            _kd = kd;
        }

        public float Compute(float setpoint, float actual, float deltaTime)
        {
            float error = setpoint - actual;
            _integral += error * deltaTime;
            float derivative = (error - _previousError) / deltaTime;
            _previousError = error;
            return _kp * error + _ki * _integral + _kd * derivative;
        }

        public void Reset()
        {
            _previousError = 0;
            _integral = 0;
        }
    }
}
using UnityEngine.InputSystem;

namespace LittleRoad
{
    public static class DrivingViewInput
    {
        public static bool LookingBehind=>Controls.Held(Controls.Act.LookBehind);
    }
}

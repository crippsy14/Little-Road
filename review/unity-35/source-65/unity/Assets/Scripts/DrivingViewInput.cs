using UnityEngine.InputSystem;

namespace LittleRoad
{
    public static class DrivingViewInput
    {
        public static bool LookingBehind=>Keyboard.current?.qKey.isPressed==true||Gamepad.current?.buttonNorth.isPressed==true;
    }
}

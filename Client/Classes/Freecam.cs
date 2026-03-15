using CitizenFX.Core;
using CitizenFX.Core.Native;
using RecM.Client;
using System;
using System.Threading.Tasks;

namespace RecM
{
    public class Freecam
    {
        #region Fields

        private static Camera _cam = null;
        private static bool _isCamFrozen = false;
        private static Vector3 _camPos = new Vector3();
        private static Vector3 _camRot = new Vector3();
        private static float _camTilt = 0;
        private static float _camFov = 45;
        private static float _camNearDOF = 0;
        private static float _camFarDOF = 70;
        private static float _camDOFStrength = 0.5f;
        private static Vector3 _camMatrixPosX = new Vector3();
        private static Vector3 _camMatrixPosY = new Vector3();
        private static Vector3 _camMatrixPosZ = new Vector3();
        public static Vector3 RaycastHitPos = new Vector3();
        public static Guid hitMarkerId = Guid.Empty;
        private static bool _isCamActive = false;
        private static Scaleform _instructionalButtons;
        private static InputMode _lastInputMode;

        #endregion

        #region Tasks

        #region Freecam handler

        private async static Task FreeCamHandler()
        {
            float speedMultiplier;

            // Fast mode
            if (Game.CurrentInputMode == InputMode.GamePad && Game.IsControlPressed((int)Game.CurrentInputMode, Control.FrontendRb) || Game.CurrentInputMode == InputMode.MouseAndKeyboard && Game.IsControlPressed((int)Game.CurrentInputMode, Control.Sprint))
                speedMultiplier = 2;

            // Slow mode
            else if (Game.CurrentInputMode == InputMode.GamePad && Game.IsControlPressed((int)Game.CurrentInputMode, Control.FrontendLb) || Game.CurrentInputMode == InputMode.MouseAndKeyboard && Game.IsControlPressed((int)Game.CurrentInputMode, Control.CharacterWheel))
                speedMultiplier = 0.050f;

            // Normal mode
            else
                speedMultiplier = 0.5f;

            if (!IsFreeCamActive())
                return;

            if (!IsFreeCamFrozen())
            {
                var camMatrix = GetFreeCamMatrix();
                var vecZ = new Vector3(0, 0, 1);
                var pos = GetFreeCamPostion();
                var rot = GetFreeCamRotation();
                var tilt = GetFreeCamTilt();
                var frameMult = Game.LastFrameTime * 60;
                var speedMult = speedMultiplier * frameMult;
                var mouseX = Game.GetDisabledControlNormal(0, Control.LookLeftRight);
                var mouseY = Game.GetDisabledControlNormal(0, Control.LookUpDown);
                var moveWS = Game.GetDisabledControlNormal(0, Control.MoveUpDown);
                var moveAD = Game.GetDisabledControlNormal(0, Control.MoveLeftRight);
                var moveQZ = (Game.CurrentInputMode == InputMode.GamePad ? Game.GetDisabledControlNormal(0, Control.ScriptedFlyZDown) : Game.GetDisabledControlNormal(0, Control.Cover)) - (Game.CurrentInputMode == InputMode.GamePad ? Game.GetDisabledControlNormal(0, Control.ScriptedFlyZUp) : Game.GetDisabledControlNormal(0, Control.MultiplayerInfo));
                var rotX = rot.X + (-mouseY * 5);
                var rotZ = rot.Z + (-mouseX * 5);
                float rotY = 0;
                pos += camMatrix[0] * moveAD * speedMult;
                pos += camMatrix[1] * -moveWS * speedMult;
                pos += vecZ * moveQZ * speedMult;
                rot = new Vector3(rotX, rotY + tilt, rotZ);
                SetFreeCamPos(pos);
                SetFreeCamRot(rot);
            }

            // Draw instructional buttons, and make sure they update if the input mode changes
            _instructionalButtons.Render2D();
            if (Game.CurrentInputMode != _lastInputMode)
            {
                _lastInputMode = Game.CurrentInputMode;
                InitialiseInstructionalButtons();
            }

            // Handle the exit button
            if (Game.IsControlJustPressed((int)Game.CurrentInputMode, Control.FrontendRright))
                SetFreeCamActive(false);

            await Task.FromResult(0);
        }

        #endregion

        #endregion

        #region Tools

        #region Freecam frozen get/set

        #region Get is cam frozen

        public static bool IsFreeCamFrozen()
        {
            return _isCamFrozen;
        }

        #endregion

        #region Set is cam frozen

        public static void SetFreeCamIsFrozen()
        {
            _isCamFrozen = !_isCamFrozen;
        }

        #endregion

        #endregion

        #region Freecam position get/set

        #region Get freecam position

        public static Vector3 GetFreeCamPostion()
        {
            return _camPos;
        }

        #endregion

        #region Set freecam postion

        public static void SetFreeCamPos(Vector3 pos)
        {
            API.LoadInterior(API.GetInteriorAtCoords(pos.X, pos.Y, pos.Z));
            API.SetFocusArea(pos.X, pos.Y, pos.Z, 0, 0, 0);
            API.LockMinimapPosition(pos.X, pos.Y);
            _cam.Position = pos;
            _camPos = pos;
        }

        #endregion

        #endregion

        #region Freecam rotation get/set

        #region Get freecam rotation

        public static Vector3 GetFreeCamRotation()
        {
            return _camRot;
        }

        #endregion

        #region Set freecam rotation

        public static void SetFreeCamRot(Vector3 rot)
        {
            rot = new Vector3(MathUtil.Clamp(rot.X, -90, 90), rot.Y % 360, rot.Z % 360);
            API.LockMinimapAngle(int.Parse(Math.Floor(rot.Z % 360).ToString()));
            GameplayCamera.RelativeHeading = MathUtil.Clamp(rot.X, -90, 90);
            GameplayCamera.RelativePitch = rot.Y % 360;
            _cam.Rotation = rot;
            _camRot = rot;
            _camMatrixPosX = (Vector3)_cam.Matrix.Row1;
            _camMatrixPosY = (Vector3)_cam.Matrix.Row2;
            _camMatrixPosZ = (Vector3)_cam.Matrix.Row3;
        }

        #endregion

        #endregion

        #region Freecam fov get/set

        #region Get freecam fov

        public static float GetFreeCamFov()
        {
            return _camFov;
        }

        #endregion

        #region Set freecam fov

        public static void SetFreeCamFov(float value)
        {
            value = MathUtil.Clamp(value, 10, 90);
            _cam.FieldOfView = value;
            _camFov = value;
        }

        #endregion

        #endregion

        #region Freecam tilt get/set

        #region Get freecam tilt

        public static float GetFreeCamTilt()
        {
            return _camTilt;
        }

        #endregion

        #region Set freecam tilt

        public static void SetFreeCamTilt(float value)
        {
            value = MathUtil.Clamp(value, -90, 90);
            _camTilt = value;
        }

        #endregion

        #endregion

        #region Freecam near DOF get/set

        #region Get freecam near DOF

        public static float GetFreeCamNearDOF()
        {
            return _camNearDOF;
        }

        #endregion

        #region Set freecam near DOF

        public static void SetFreeCamNearDOF(float value)
        {
            value = MathUtil.Clamp(value, 0, 72);
            API.SetCamNearDof(_cam.Handle, value);
            _camNearDOF = value;
        }

        #endregion

        #endregion

        #region Freecam far DOF get/set

        #region Get freecam far DOF

        public static float GetFreeCamFarDOF()
        {
            return _camFarDOF;
        }

        #endregion

        #region Set freecam far DOF

        public static void SetFreeCamFarDOF(float value)
        {
            value = MathUtil.Clamp(value, 0, 300);
            API.SetCamFarDof(_cam.Handle, value);
            _camFarDOF = value;
        }

        #endregion

        #endregion

        #region Freecam DOF strength get/set

        #region Get freecam DOF strength

        public static float GetFreeCamDOFStrength()
        {
            return _camDOFStrength;
        }

        #endregion

        #region Set freecam DOF strength

        public static void SetFreeCamDOFStrength(float value)
        {
            value = MathUtil.Clamp(value, 0, 1);
            API.SetCamDofStrength(_cam.Handle, value);
            _camDOFStrength = value;
        }

        #endregion

        #endregion

        #region Get freecam matrix

        public static Vector3[] GetFreeCamMatrix()
        {
            return new Vector3[] { _camMatrixPosX, _camMatrixPosY, _camMatrixPosZ };
        }

        #endregion

        #region Get freecam target

        public static Vector3 GetFreeCamTarget(Vector3 distance)
        {
            return _camPos + (_camMatrixPosY * distance);
        }

        #endregion

        #region Freecam states get/set

        #region Get is freecam active

        public static bool IsFreeCamActive()
        {
            return _isCamActive;
        }

        #endregion

        #region Set freecam active

        public static void SetFreeCamActive(bool state)
        {
            _isCamActive = state;
            if (_isCamActive)
            {
                _cam = new Camera(API.CreateCam("DEFAULT_SCRIPTED_CAMERA", true));
                SetFreeCamFov(45);
                SetFreeCamPos(GameplayCamera.Position);
                SetFreeCamRot(GameplayCamera.Rotation);
                if (Game.PlayerPed.IsInVehicle())
                    API.SetVehicleRadioEnabled(Game.PlayerPed.CurrentVehicle.Handle, false);
                API.SetPlayerControl(Game.Player.Handle, false, 260);
                InitialiseInstructionalButtons();
                Main.Instance.AttachTick(FreeCamHandler);
            }
            else
            {
                _cam.Delete();
                API.ClearFocus();
                API.UnlockMinimapPosition();
                API.UnlockMinimapAngle();
                GameplayCamera.RelativePitch = 4;
                GameplayCamera.RelativeHeading = 0;
                if (Game.PlayerPed.IsInVehicle())
                    API.SetVehicleRadioEnabled(Game.PlayerPed.CurrentVehicle.Handle, true);
                API.SetPlayerControl(Game.Player.Handle, true, 260);
                Main.Instance.DetachTick(FreeCamHandler);
            }

            API.RenderScriptCams(_isCamActive, true, 1000, true, true);
        }

        #endregion

        #endregion

        #region Instructional buttons

        private static void InitialiseInstructionalButtons()
        {
            if (_instructionalButtons != null)
                _instructionalButtons.Dispose();
            _instructionalButtons = new Scaleform("INSTRUCTIONAL_BUTTONS");
            _instructionalButtons.CallFunction("CLEAR_ALL");
            if (Game.CurrentInputMode == InputMode.MouseAndKeyboard)
            {
                _instructionalButtons.CallFunction("SET_DATA_SLOT", 0, API.GetControlInstructionalButton(0, (int)Control.FrontendRright, 0), "Exit");
                _instructionalButtons.CallFunction("SET_DATA_SLOT", 1, API.GetControlInstructionalButton(0, (int)Control.Sprint, 0), API.GetControlInstructionalButton(0, (int)Control.CharacterWheel, 0), "Slow/Fast");
                _instructionalButtons.CallFunction("SET_DATA_SLOT", 2, API.GetControlInstructionalButton(0, (int)Control.Cover, 0), API.GetControlInstructionalButton(0, (int)Control.MultiplayerInfo, 0), "Height");
                _instructionalButtons.CallFunction("SET_DATA_SLOT", 3, API.GetControlInstructionalButton(0, (int)Control.MoveRightOnly, 0), API.GetControlInstructionalButton(0, (int)Control.MoveLeftOnly, 0), API.GetControlInstructionalButton(0, (int)Control.MoveDownOnly, 0), API.GetControlInstructionalButton(0, (int)Control.MoveUpOnly, 0), "Move");
            }
            else
            {
                _instructionalButtons.CallFunction("SET_DATA_SLOT", 0, API.GetControlInstructionalButton(0, (int)Control.FrontendRright, 0), "Exit");
                _instructionalButtons.CallFunction("SET_DATA_SLOT", 1, API.GetControlInstructionalButton(0, (int)Control.FrontendRb, 0), API.GetControlInstructionalButton(0, (int)Control.FrontendLb, 0), "Slow/Fast");
                _instructionalButtons.CallFunction("SET_DATA_SLOT", 2, API.GetControlInstructionalButton(0, (int)Control.ScriptedFlyZDown, 0), API.GetControlInstructionalButton(0, (int)Control.ScriptedFlyZUp, 0), "Height");
                _instructionalButtons.CallFunction("SET_DATA_SLOT", 3, API.GetControlInstructionalButton(0, 7, 0), "Camera");
                _instructionalButtons.CallFunction("SET_DATA_SLOT", 4, API.GetControlInstructionalButton(0, 28, 0), "Move");
            }
            _instructionalButtons.CallFunction("DRAW_INSTRUCTIONAL_BUTTONS", 0);
        }

        #endregion

        #endregion
    }
}
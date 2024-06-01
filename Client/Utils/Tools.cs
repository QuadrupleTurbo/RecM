using System;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;

namespace RecM.Client.Utils
{
    internal class Tools
    {
        #region Spawn vehicle

        public static async Task<Vehicle> SpawnVehicle(string model, bool networked = true)
        {
            // Store the data before the vehicle's deleted
            Entity entity = Game.PlayerPed.CurrentVehicle != null && Game.PlayerPed.CurrentVehicle.Exists() ? (Entity)Game.PlayerPed.CurrentVehicle : Game.PlayerPed;
            Vector3 pos = entity.Position;
            float heading = entity.Heading;

            // Make sure that the model's loaded correctly
            var success = await LoadModel(model);
            if (!success)
            {
                $"The model is invalid!".Error(true);
                return null;
            }

            // Delete the last vehicle if it exists
            Game.PlayerPed.CurrentVehicle?.Delete();
            Game.PlayerPed.LastVehicle?.Delete();

            Vehicle veh = null;
            if (networked)
            {
                veh = await World.CreateVehicle(model, pos, heading);
                if (veh == null)
                {
                    veh = await World.CreateVehicle(model, pos, heading);
                }
            }
            else
            {
                var currTime = Main.GameTime;
                int handle = API.CreateVehicle(Game.GenerateHashASCII(model), pos.X, pos.Y, pos.Z, heading, false, false);
                while (handle == 0)
                {
                    // If the timer exceeds this time to wait, break the loop
                    if (Main.GameTime - currTime > 20000)
                        break;

                    handle = API.CreateVehicle(Game.GenerateHashASCII(model), pos.X, pos.Y, pos.Z, heading, false, false);
                    await BaseScript.Delay(0);
                }

                if (handle != 0)
                    veh = new Vehicle(handle);
            }

            // No point of continuing without a vehicle
            if (veh == null)
            {
                $"The vehicle was null and couldn't be spawned.".Error(true);
                return null;
            }

            // Misc settings
            veh.RadioStation = RadioStation.RadioOff;
            veh.IsInvincible = true;
            veh.CanBeVisiblyDamaged = false;
            veh.IsEngineRunning = true;

            // Set the player into the vehicle
            Game.Player.Character.SetIntoVehicle(veh, VehicleSeat.Driver);

            // So that the game eventually deletes it when player's away
            veh.Model.MarkAsNoLongerNeeded();

            return veh;
        }

        #endregion

        #region Load Model

        /// <summary>
        /// Request the model with a timeout (in seconds) or if you don't want a timeout, set timeout null.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="timeout"></param>
        /// <returns>Whether the model was requested successfully.</returns>
        public static async Task<bool> LoadModel(Model model, int? timeout = null)
        {
            if (model.IsValid && model.IsVehicle)
            {
                if (timeout != null)
                    return await model.Request((int)timeout * 1000);
                else
                {
                    model.Request();
                    while (!model.IsLoaded)
                        await BaseScript.Delay(0);
                    return true;
                }
            }
            else
                return false;
        }

        #endregion

        #region Teleport

        /// <summary>
        /// A custom teleport function to teleport an entity.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="heading"></param>
        /// <returns></returns>
        public static async Task Teleport(Vector3 pos, float heading, bool fade, bool safeMode = false)
        {
            if (fade)
            {
                if (Screen.Fading.IsFadedIn || Screen.Fading.IsFadingIn)
                {
                    Screen.Fading.FadeOut(500);
                    while (!Screen.Fading.IsFadedOut)
                        await BaseScript.Delay(0);
                }
            }

            // Request collisions and start a new scene
            API.RequestCollisionAtCoord(pos.X, pos.Y, pos.Z);

            // Wait for the scene to load
            API.SetFocusPosAndVel(pos.X, pos.Y, pos.Z, 0, 0, 0);
            API.NewLoadSceneStart(pos.X, pos.Y, pos.Z, pos.X, pos.Y, pos.Z, 50f, 0);

            // The current time
            long currTime;

            // Ped is always constant
            var ped = Game.PlayerPed;
            Entity entity = ped.IsInVehicle() && ped.CurrentVehicle != null ? ped.CurrentVehicle : ped;
            if (entity is Vehicle)
            {
                Vehicle veh = entity as Vehicle;
                if (veh.Driver == ped)
                {
                    // Add an offset to the Z coord for it being a vehicle
                    pos = new Vector3(pos.X, pos.Y, pos.Z + 2);

                    // Set position of the entity
                    veh.Position = pos;

                    // Set entity heading
                    veh.Heading = heading;
                }
            }
            else
            {
                // Set position of the entity
                ped.Position = pos;

                // Set entity heading
                ped.Heading = heading;

                // Fall through the map without this...
                ped.IsPositionFrozen = true;

                // Wait for the collision to load
                currTime = Main.GameTime;
                while (!API.HasCollisionLoadedAroundEntity(ped.Handle))
                {
                    // If the timer exceeds this time to wait, break the loop
                    if (Main.GameTime - currTime > 20000)
                    {
                        //"Saved a hang on finding a non existent collision!".Log();
                        break;
                    }

                    API.RequestCollisionAtCoord(pos.X, pos.Y, pos.Z);
                    await BaseScript.Delay(0);
                }

                // Now release the player
                ped.IsPositionFrozen = false;

                // To make sure the player's not floating in the air like a fucking wizard
                ped.Task.ClearAllImmediately();
            }

            // Set the correct pitch of the gameplay camera
            GameplayCamera.RelativePitch = 4;

            // Set the correct heading of the gameplay camera
            GameplayCamera.RelativeHeading = 0;

            // Let's wait for the scene to be loaded then stop it
            currTime = Main.GameTime;
            while (!API.IsNewLoadSceneLoaded() && Main.GameTime - currTime < 20000)
            {
                //"Waiting for the scene to be loaded...".Log();
                await BaseScript.Delay(0);
            }
            API.ClearFocus();
            API.NewLoadSceneStop();

            if (fade)
            {
                if (Screen.Fading.IsFadedOut || Screen.Fading.IsFadingOut)
                {
                    Screen.Fading.FadeIn(800);
                    while (!Screen.Fading.IsFadedIn)
                        await BaseScript.Delay(0);
                }
            }
        }

        #endregion

        #region Format timer

        public static string FormatTimer(int start, int curr)
        {
            int newTime;

            if (curr == 0) newTime = start;
            else newTime = curr - start;

            var ms = Math.Floor((double)newTime % 1000);
            var seconds = Math.Floor((double)newTime / 1000);
            var minutes = Math.Floor(seconds / 60); seconds = Math.Floor(seconds % 60);

            return string.Format("{0:0}:{1:00}:{2:000}", minutes, seconds, ms);
        }

        #endregion

        #region GetUserInput

        /// <summary>
        /// Get a user input text string.
        /// </summary>
        /// <returns></returns>
        public static async Task<string> GetUserInput() => await GetUserInput(null, null, 30);

        /// <summary>
        /// Get a user input text string.
        /// </summary>
        /// <param name="maxInputLength"></param>
        /// <returns></returns>
        public static async Task<string> GetUserInput(int maxInputLength) => await GetUserInput(null, null, maxInputLength);

        /// <summary>
        /// Get a user input text string.
        /// </summary>
        /// <param name="windowTitle"></param>
        /// <returns></returns>
        public static async Task<string> GetUserInput(string windowTitle) => await GetUserInput(windowTitle, null, 30);

        /// <summary>
        /// Get a user input text string.
        /// </summary>
        /// <param name="windowTitle"></param>
        /// <param name="maxInputLength"></param>
        /// <returns></returns>
        public static async Task<string> GetUserInput(string windowTitle, int maxInputLength) => await GetUserInput(windowTitle, null, maxInputLength);

        /// <summary>
        /// Get a user input text string.
        /// </summary>
        /// <param name="windowTitle"></param>
        /// <param name="defaultText"></param>
        /// <returns></returns>
        public static async Task<string> GetUserInput(string windowTitle, string defaultText) => await GetUserInput(windowTitle, defaultText, 30);

        /// <summary>
        /// Get a user input text string.
        /// </summary>
        /// <param name="windowTitle"></param>
        /// <param name="defaultText"></param>
        /// <param name="maxInputLength"></param>
        /// <returns></returns>
        public static async Task<string> GetUserInput(string windowTitle, string defaultText, int maxInputLength)
        {
            // Create the window title string.
            var spacer = "\t";
            API.AddTextEntry($"{API.GetCurrentResourceName().ToUpper()}_WINDOW_TITLE", $"{windowTitle ?? "Enter"}:{spacer}" /*+ "(MAX {maxInputLength} Characters)"*/);

            // Display the input box.
            API.DisplayOnscreenKeyboard(1, $"{API.GetCurrentResourceName().ToUpper()}_WINDOW_TITLE", "", defaultText ?? "", "", "", "", maxInputLength);
            await BaseScript.Delay(0);

            // Wait for a result.
            while (true)
            {
                int keyboardStatus = API.UpdateOnscreenKeyboard();
                DisableMovementControlsThisFrame(true, true);

                switch (keyboardStatus)
                {
                    case 3: // not displaying input field anymore somehow
                    case 2: // cancelled
                        return null;

                    case 1: // finished editing
                        return API.GetOnscreenKeyboardResult();

                    default:
                        await BaseScript.Delay(0);
                        break;
                }
            }
        }

        #endregion

        #region Disable Movement Controls

        /// <summary>
        /// Disables all movement and camera related controls this frame.
        /// </summary>
        /// <param name="disableMovement"></param>
        /// <param name="disableCameraMovement"></param>
        public static void DisableMovementControlsThisFrame(bool disableMovement, bool disableCameraMovement)
        {
            if (disableMovement)
            {
                Game.DisableControlThisFrame(0, Control.MoveDown);
                Game.DisableControlThisFrame(0, Control.MoveDownOnly);
                Game.DisableControlThisFrame(0, Control.MoveLeft);
                Game.DisableControlThisFrame(0, Control.MoveLeftOnly);
                Game.DisableControlThisFrame(0, Control.MoveLeftRight);
                Game.DisableControlThisFrame(0, Control.MoveRight);
                Game.DisableControlThisFrame(0, Control.MoveRightOnly);
                Game.DisableControlThisFrame(0, Control.MoveUp);
                Game.DisableControlThisFrame(0, Control.MoveUpDown);
                Game.DisableControlThisFrame(0, Control.MoveUpOnly);
                Game.DisableControlThisFrame(0, Control.VehicleFlyMouseControlOverride);
                Game.DisableControlThisFrame(0, Control.VehicleMouseControlOverride);
                Game.DisableControlThisFrame(0, Control.VehicleMoveDown);
                Game.DisableControlThisFrame(0, Control.VehicleMoveDownOnly);
                Game.DisableControlThisFrame(0, Control.VehicleMoveLeft);
                Game.DisableControlThisFrame(0, Control.VehicleMoveLeftRight);
                Game.DisableControlThisFrame(0, Control.VehicleMoveRight);
                Game.DisableControlThisFrame(0, Control.VehicleMoveRightOnly);
                Game.DisableControlThisFrame(0, Control.VehicleMoveUp);
                Game.DisableControlThisFrame(0, Control.VehicleMoveUpDown);
                Game.DisableControlThisFrame(0, Control.VehicleSubMouseControlOverride);
                Game.DisableControlThisFrame(0, Control.Duck);
                Game.DisableControlThisFrame(0, Control.SelectWeapon);
            }
            if (disableCameraMovement)
            {
                Game.DisableControlThisFrame(0, Control.LookBehind);
                Game.DisableControlThisFrame(0, Control.LookDown);
                Game.DisableControlThisFrame(0, Control.LookDownOnly);
                Game.DisableControlThisFrame(0, Control.LookLeft);
                Game.DisableControlThisFrame(0, Control.LookLeftOnly);
                Game.DisableControlThisFrame(0, Control.LookLeftRight);
                Game.DisableControlThisFrame(0, Control.LookRight);
                Game.DisableControlThisFrame(0, Control.LookRightOnly);
                Game.DisableControlThisFrame(0, Control.LookUp);
                Game.DisableControlThisFrame(0, Control.LookUpDown);
                Game.DisableControlThisFrame(0, Control.LookUpOnly);
                Game.DisableControlThisFrame(0, Control.ScaledLookDownOnly);
                Game.DisableControlThisFrame(0, Control.ScaledLookLeftOnly);
                Game.DisableControlThisFrame(0, Control.ScaledLookLeftRight);
                Game.DisableControlThisFrame(0, Control.ScaledLookUpDown);
                Game.DisableControlThisFrame(0, Control.ScaledLookUpOnly);
                Game.DisableControlThisFrame(0, Control.VehicleDriveLook);
                Game.DisableControlThisFrame(0, Control.VehicleDriveLook2);
                Game.DisableControlThisFrame(0, Control.VehicleLookBehind);
                Game.DisableControlThisFrame(0, Control.VehicleLookLeft);
                Game.DisableControlThisFrame(0, Control.VehicleLookRight);
                Game.DisableControlThisFrame(0, Control.NextCamera);
                Game.DisableControlThisFrame(0, Control.VehicleFlyAttackCamera);
                Game.DisableControlThisFrame(0, Control.VehicleCinCam);
            }
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;

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
                $"The vehicle was null and couldn't be spawned.".Error();
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
    }
}

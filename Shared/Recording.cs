using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

#if CLIENT

using RecM.Client;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using FxEvents;

#endif

#if SERVER

using System.IO;
using System.Xml;
using RecM.Server;
using CodeWalker.GameFiles;
using CitizenFX.Core.Native;
using CitizenFX.Core;

#endif

namespace RecM
{
    public class Recording
    {
        #region Fields

#if CLIENT

        /// <summary>
        /// The current recording data.
        /// </summary>
        private static readonly List<Record> _currRecording = [];

        /// <summary>
        /// The start time of the recording.
        /// </summary>
        private static int _recordingStartTime;

#endif

        #endregion

        #region Constructor

#if CLIENT

        public Recording()
        {
            API.RegisterCommand("record", new Action<int, List<object>, string>(async (source, args, rawCommand) =>
            {
                if (args[0].ToString() == "start")
                {
                    Main.Instance.AttachTick(RecordingThread);
                    return;
                }
                else if (args[0].ToString() == "stop")
                {
                    Main.Instance.DetachTick(RecordingThread);
                    return;
                }
                else if (args[0].ToString() == "save")
                {
                    if (args.Count < 2)
                    {
                        "You need to specify a name for the recording.".Error();
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(args[1].ToString()))
                    {
                        "You need to specify a name for the recording.".Error();
                        return;
                    }

                    if (args[1].ToString().Contains("_"))
                    {
                        "The recording name cannot contain underscores.".Error();
                        return;
                    }

                    await SaveRecording(args[1].ToString());
                }
                else if (args[0].ToString() == "overwrite")
                {
                    if (args.Count < 2)
                    {
                        "You need to specify a name for the recording.".Error();
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(args[1].ToString()))
                    {
                        "You need to specify a name for the recording.".Error();
                        return;
                    }

                    if (args[1].ToString().Contains("_"))
                    {
                        "The recording name cannot contain underscores.".Error();
                        return;
                    }

                    await SaveRecording(args[1].ToString(), true);
                }
                else if (args[0].ToString() == "delete")
                {
                    if (args.Count < 3)
                    {
                        "You need to specify the name & the model for the recording.".Error();
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(args[1].ToString()) || string.IsNullOrWhiteSpace(args[2].ToString()))
                    {
                        "You need to specify the name & the model for the recording.".Error();
                        return;
                    }

                    await DeleteRecording(args[1].ToString(), "ariant");
                }
                else if (args[0].ToString() == "get")
                {
                    var recordings = await GetRecordings();
                    
                    if (recordings.Count == 0)
                    {
                        "There are no recordings.".Error();
                        return;
                    }   

                    string.Join("\n", recordings).Log();
                }
                else
                {
                    "Invalid command.".Error();
                }
            }), false);

            // Only at startup
            LoadTextures();
        }

#endif

#if SERVER

        public Recording()
        {
            Main.Instance.AddEventHandler("RecM:saveRecording:Server", new Action<string, string, string, bool, NetworkCallbackDelegate>(SaveRecording), true);
            Main.Instance.AddEventHandler("RecM:deleteRecording:Server", new Func<Player, string, string, Task<Tuple<bool, string>>>(DeleteRecording));
            Main.Instance.AddEventHandler("RecM:getRecordings:Server", new Func<Player, Task<List<string>>>(GetRecordings));

            // Only at startup
            CleanRecordings();
        }

#endif

#endregion

        #region Events

#if SERVER

        #region Save recording

        private void SaveRecording(string name, string model, string data, bool overwrite, NetworkCallbackDelegate cb)
        {
            try
            {
                // If the resource doesn't exist
                if (!Directory.Exists(API.GetResourcePath("RecM_records")))
                {
                    "You need to have the RecM_records resource installed.".Error();
                    cb(false, "You need to have the RecM_records resource installed.");
                    return;
                }

                // Parse the data
                var recordings = Json.Parse<List<Record>>(data);

                // This will be used to load/save meta files
                XmlDocument doc = new();

                // Load the xml
                doc.LoadXml("<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                    "\n<VehicleRecordList>" +
                    "\n</VehicleRecordList>");

                // Loop through the recordings and add the children to the xml
                foreach (Record recording in recordings)
                {
                    // Declare the item element
                    XmlElement itemElement = doc.CreateElement("Item");

                    // Time
                    XmlElement timeElement = doc.CreateElement("Time");
                    timeElement.SetAttribute("value", recording.Time.ToString());
                    itemElement.AppendChild(timeElement);

                    // Position
                    XmlElement posElement = doc.CreateElement("Position");
                    posElement.SetAttribute("x", recording.Position.X.ToString());
                    posElement.SetAttribute("y", recording.Position.Y.ToString());
                    posElement.SetAttribute("z", recording.Position.Z.ToString());
                    itemElement.AppendChild(posElement);

                    // Velocity
                    XmlElement velElement = doc.CreateElement("Velocity");
                    velElement.SetAttribute("x", recording.Velocity.X.ToString());
                    velElement.SetAttribute("y", recording.Velocity.Y.ToString());
                    velElement.SetAttribute("z", recording.Velocity.Z.ToString());
                    itemElement.AppendChild(velElement);

                    // Top/Forward
                    XmlElement topElement = doc.CreateElement("Forward");
                    topElement.SetAttribute("x", recording.Forward.X.ToString());
                    topElement.SetAttribute("y", recording.Forward.Y.ToString());
                    topElement.SetAttribute("z", recording.Forward.Z.ToString());
                    itemElement.AppendChild(topElement);

                    // Right
                    XmlElement rightElement = doc.CreateElement("Right");
                    rightElement.SetAttribute("x", recording.Right.X.ToString());
                    rightElement.SetAttribute("y", recording.Right.Y.ToString());
                    rightElement.SetAttribute("z", recording.Right.Z.ToString());
                    itemElement.AppendChild(rightElement);

                    // Steering
                    XmlElement steerElement = doc.CreateElement("Steering");
                    steerElement.SetAttribute("value", recording.SteeringAngle.ToString("F4"));
                    itemElement.AppendChild(steerElement);

                    // Gas
                    XmlElement gasElement = doc.CreateElement("GasPedal");
                    gasElement.SetAttribute("value", recording.Gas.ToString("F4"));
                    itemElement.AppendChild(gasElement);

                    // Brake
                    XmlElement brakeElement = doc.CreateElement("BrakePedal");
                    brakeElement.SetAttribute("value", recording.Brake.ToString("F4"));
                    itemElement.AppendChild(brakeElement);

                    // Handbrake
                    XmlElement handbrakeElement = doc.CreateElement("Handbrake");
                    handbrakeElement.SetAttribute("value", recording.UseHandbrake.ToString());
                    itemElement.AppendChild(handbrakeElement);

                    // Finally add the item element
                    doc["VehicleRecordList"].AppendChild(itemElement);
                }

                // Now, convert to proper yvr format
                var yvr = XmlYvr.GetYvr(doc);
                var yvrData = yvr.Save();
                RpfFile.LoadResourceFile(yvr, yvrData, 1);

                // Move it to the recordings resource
                var recordingsPath = Path.Combine(API.GetResourcePath("RecM_records"), "stream");
                if (!File.Exists(Path.Combine(recordingsPath, $"{name}_{model}_001.yvr")))
                    File.WriteAllBytes(Path.Combine(recordingsPath, $"{name}_{model}_001.yvr"), yvrData);
                else
                {
                    // Optional depending on the context
                    if (overwrite)
                    {
                        // Now if we're here, the file existed, now we need to find the highest number and add 1 to it
                        var maxValue = Directory.EnumerateFiles(recordingsPath)
                            .Where(x => Path.GetFileNameWithoutExtension(x).StartsWith($"{name}_{model}"))
                            .Select(x => int.Parse(Path.GetFileNameWithoutExtension(x).Split('_')[2]))
                            .Max();
                        File.WriteAllBytes(Path.Combine(recordingsPath, $"{name}_{model}_{(maxValue + 1).ToString().PadLeft(3, '0')}.yvr"), yvrData);
                    }
                    else                     
                    {
                        cb(false, "A recording with this name and model already exists.");
                        return;
                    }
                }

                // Finally, refresh and restart the recordings resource (hopefully no problems client side)
                API.ExecuteCommand("refresh");
                API.ExecuteCommand("ensure RecM_records");

                // Callback telling the client that the recording was saved
                cb(true, "Success!");
            }
            catch (Exception ex)
            {
                ex.ToString().Error();
                cb(false, "There was an error with saving the recording, please check the server console for an exception.");
            }
        }

        #endregion

        #region Delete recording

        private async Task<Tuple<bool, string>> DeleteRecording([FromSource] Player source, string name, string model)
        {
            try
            {
                // If the resource doesn't exist
                if (!Directory.Exists(API.GetResourcePath("RecM_records")))
                {
                    "You need to have the RecM_records resource installed.".Error();
                    return new Tuple<bool, string>(false, "You need to have the RecM_records resource installed.");
                }

                // The path to the recordings
                var recordingsPath = Path.Combine(API.GetResourcePath("RecM_records"), "stream");

                // Delete all files that start with the name and model
                foreach (var file in Directory.EnumerateFiles(recordingsPath))
                {
                    // Looking for all of them since there might be multiple recordings with the same name and model due to the overwriting process
                    if (Path.GetFileNameWithoutExtension(file).StartsWith($"{name}_{model}"))
                        File.Delete(file);
                }

                // Refresh and restart the recordings resource (hopefully no problems client side)
                API.ExecuteCommand("refresh");
                API.ExecuteCommand("ensure RecM_records");

                // Finally, callback telling the client that the recording was saved
                return new Tuple<bool, string>(true, "Success!");
            }
            catch (Exception ex)
            {
                ex.ToString().Error();
                return new Tuple<bool, string>(false, "There was an error with deleting the recording, please check the server console for an exception.");
            }
        }

        #endregion

        #region Get recordings

        private async Task<List<string>> GetRecordings([FromSource] Player source)
        {
            try
            {
                // If the resource doesn't exist
                if (!Directory.Exists(API.GetResourcePath("RecM_records")))
                {
                    "You need to have the RecM_records resource installed.".Error();
                    return [];
                }

                // The path to the recordings
                var recordingsPath = Path.Combine(API.GetResourcePath("RecM_records"), "stream");

                // Find all files that start with the name and model
                List<string> recordings = [];
                foreach (var file in Directory.EnumerateFiles(recordingsPath))
                {
                    var name = Path.GetFileNameWithoutExtension(file).Split('_')[0];
                    var model = Path.GetFileNameWithoutExtension(file).Split('_')[1];
                    var num = Path.GetFileNameWithoutExtension(file).Split('_')[2];
                    var maxValue = Directory.EnumerateFiles(recordingsPath)
                        .Where(x => Path.GetFileNameWithoutExtension(x).StartsWith($"{name}_{model}"))
                        .Select(x => int.Parse(Path.GetFileNameWithoutExtension(x).Split('_')[2]))
                        .Max();

                    // Disregard the duplicates that are below the highest id
                    if (num == maxValue.ToString().PadLeft(3, '0'))
                        recordings.Add(Path.GetFileNameWithoutExtension(file));
                }

                return recordings;
            }
            catch (Exception ex)
            {
                ex.ToString().Error();
                return [];
            }
        }

        #endregion

#endif

        #endregion

        #region Ticks

#if CLIENT

        private async Task RecordingThread()
        {
            RecordThisFrame();
            await BaseScript.Delay(0);
        }

#endif

        #endregion

        #region Tools

#if CLIENT

        #region Record this frame

        public static void RecordThisFrame()
        {
            if (!Game.PlayerPed.IsInVehicle()) return;
            if (Game.PlayerPed.CurrentVehicle == null) return;
            if (!Game.PlayerPed.CurrentVehicle.Exists()) return;
            if (Game.PlayerPed.CurrentVehicle.GetPedOnSeat(VehicleSeat.Driver) != Game.PlayerPed) return;
            var veh = Game.PlayerPed.CurrentVehicle;

            // We only need this once per recording obviously
            if (_recordingStartTime == 0)
                _recordingStartTime = Game.GameTime;

            Vector3 forward = new();
            Vector3 right = new();
            Vector3 _ = new();
            Vector3 pos = new();
            API.GetEntityMatrix(veh.Handle, ref forward, ref right, ref _, ref pos);

            _currRecording.Add(new Record()
            {
                Time = Game.GameTime - _recordingStartTime,
                Position = pos,
                Velocity = veh.Velocity,
                Right = right,
                Forward = forward,
                SteeringAngle = (float)(API.GetVehicleSteeringAngle(veh.Handle) * Math.PI / 180),
                Gas = API.GetControlNormal(0, (int)Control.VehicleAccelerate),
                Brake = API.GetControlNormal(0, (int)Control.VehicleBrake),
                UseHandbrake = API.GetVehicleHandbrake(veh.Handle)
            });
        }

        #endregion

        #region Save recording

        public static async Task<bool> SaveRecording(string name, bool overwrite = false)
        {
            if (_currRecording.Count == 0)
            {
                "You need to record something first!".Error();
                return false;
            }
            if (!Game.PlayerPed.IsInVehicle()) return false;
            if (Game.PlayerPed.CurrentVehicle == null) return false;
            if (!Game.PlayerPed.CurrentVehicle.Exists()) return false;
            if (Game.PlayerPed.CurrentVehicle.GetPedOnSeat(VehicleSeat.Driver) != Game.PlayerPed) return false;
            var veh = Game.PlayerPed.CurrentVehicle;

            // Create a TaskCompletionSource to await the event completion
            var tcs = new TaskCompletionSource<bool>();

            // Latent event that sends little increments of data to the server
            BaseScript.TriggerLatentServerEvent("RecM:saveRecording:Server", 200000, name, veh.DisplayName, Json.Stringify(_currRecording), overwrite, new Action<bool, string>((success, msg) =>
            {
                if (!success)
                {
                    msg.Error();
                    tcs.SetResult(false);
                    return;
                }

                // Notify the client of the recording being saved
                "Recording saved!".Log();

                // Reset the recording data
                _currRecording.Clear();
                _recordingStartTime = 0;

                tcs.SetResult(true);
            }));

            return await tcs.Task;
        }

        #endregion

        #region Delete recording

        public static async Task<bool> DeleteRecording(string name, string model)
        {
            // Latent event that sends little increments of data to the server
            (bool success, string msg) = await EventDispatcher.Get<Tuple<bool, string>>("RecM:deleteRecording:Server", name, model);
            if (!success)
            {
                msg.Error();
                return false;
            }

            // Notify the client of the recording being deleted
            "Recording deleted!".Log();

            return true;
        }

        #endregion

        #region Get recordings

        public static async Task<List<string>> GetRecordings()
        {
            // Get the list of recordings
            List<string> recordings = await EventDispatcher.Get<List<string>>("RecM:getRecordings:Server");

            // The server will return the default value if there's an error
            if (recordings == default)
            {
                "There was an error with getting the recordings, please check the server console for an exception.".Error();
                return default;
            }

            return recordings;
        }

        #endregion

        #region Load textures

        /// <summary>
        /// Loads the textures that are supplied with the resource.
        /// </summary>
        private async void LoadTextures()
        {
            var currTime = Main.GameTime;
            while (!API.HasStreamedTextureDictLoaded("recm_textures") && Main.GameTime - currTime < 7000) // With a timeout of 7 seconds
            {
                API.RequestStreamedTextureDict("recm_textures", false);
                await BaseScript.Delay(0);
            }
        }

        #endregion

#endif

#if SERVER

        #region Clean recordings

        /// <summary>
        /// Mostly for the overwrites, since when overwriting, it'll create a duplicate with a higher recording id, so we can use this to reset them back to the lowest id.
        /// </summary>
        private static void CleanRecordings()
        {
            try
            {
                // If the resource doesn't exist
                if (!Directory.Exists(API.GetResourcePath("RecM_records")))
                {
                    "You need to have the RecM_records resource installed.".Error();
                    return;
                }

                // The path to the recordings
                var recordingsPath = Path.Combine(API.GetResourcePath("RecM_records"), "stream");

                // Find all files that are older than the current
                List<string> filesToKeep = [];
                List<string> filesToDestroy = [];
                foreach (var file in Directory.EnumerateFiles(recordingsPath))
                {
                    var name = Path.GetFileNameWithoutExtension(file).Split('_')[0];
                    var model = Path.GetFileNameWithoutExtension(file).Split('_')[1];
                    var num = Path.GetFileNameWithoutExtension(file).Split('_')[2];
                    var maxValue = Directory.EnumerateFiles(recordingsPath)
                        .Where(x => Path.GetFileNameWithoutExtension(x).StartsWith($"{name}_{model}"))
                        .Select(x => int.Parse(Path.GetFileNameWithoutExtension(x).Split('_')[2]))
                        .DefaultIfEmpty(0)
                        .Max();

                    // Delete the file if there's a duplicate with a lower recording id
                    if (num != maxValue.ToString().PadLeft(3, '0'))
                    {
                        File.Delete(file);
                        continue;
                    }

                    // Otherwise, rename the file to the lowest recording id value
                    File.Move(file, Path.Combine(Path.GetDirectoryName(file), $"{name}_{model}_001.yvr"));
                }

                // Finally, refresh and ensure the recordings resource
                API.ExecuteCommand("refresh");
                API.ExecuteCommand("ensure RecM_records");
            }
            catch (Exception ex)
            {
                ex.ToString().Error();
            }
        }

        #endregion

#endif

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Drawing;
using FxEvents.Shared.TypeExtensions;

#if CLIENT

using RecM.Client;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;
using FxEvents;
using RecM.Client.Utils;

#endif

#if SERVER

using System.IO;
using System.Xml;
using RecM.Server;
using CodeWalker.GameFiles;
using CitizenFX.Core.Native;
using CitizenFX.Core;
using FxEvents;

#endif

namespace RecM
{
    public class Recording
    {
        #region Fields

#if CLIENT

        /// <summary>
        /// To indicate if the recording is loaded or not.
        /// </summary>
        public static bool IsLoadingRecording;

        /// <summary>
        /// Whether the player's recording a playback.
        /// </summary>
        public static bool IsRecording;

        /// <summary>
        /// The default vehicle to use if the current vehicle or model doesn't exist.
        /// </summary>
        private static readonly string _defaultVehicle = "dubsta2";

        /// <summary>
        /// The current recording data.
        /// </summary>
        private static readonly List<Record> _currRecording = [];

        /// <summary>
        /// The start time of the recording.
        /// </summary>
        private static int _recordingStartTime;

        /// <summary>
        /// The last recording played, key being the id, and value being the name.
        /// </summary>
        private static Tuple<int, string> _lastRecordingPlayed;

        /// <summary>
        /// The current recording playback start time.
        /// </summary>
        private static int _currRecordingPlaybackStartTime;

        /// <summary>
        /// The current recording duration.
        /// </summary>
        private static int _currRecordingDuration;

        /// <summary>
        /// The current positions saved throughout the current recording.
        /// </summary>
        private static List<Vector3> _currRecordingPositions = [];

        /// <summary>
        /// The last vehicle of the player.
        /// </summary>
        private static string _lastVehicleModel;

        /// <summary>
        /// The last location of the player.
        /// </summary>
        private static Vector4? _lastLocation = null;

        /// <summary>
        /// Just adds a cooldown after the recording is played.
        /// </summary>
        private static bool _recordingCooldown = false;

        /// <summary>
        /// This has to store the client's original cinematic cam state.
        /// </summary>
        private static bool _cinematicCamBlocked;

#endif

        #endregion

        #region Constructor

#if CLIENT

        public Recording()
        {
            Main.Instance.AddEventHandler("RecM:registerRecording:Client", new Action<string, string>(RegisterRecording));
            Main.Instance.AttachTick(GeneralThread);

            // Only at startup
            LoadTextures();
        }

#endif

#if SERVER

        public Recording()
        {
            Main.Instance.AddEventHandler("RecM:saveRecording:Server", new Action<string, string, string, bool, NetworkCallbackDelegate>(SaveRecording), true);
            Main.Instance.AddEventHandler("RecM:deleteRecording:Server", new Func<Player, string, string, Task<Tuple<bool, string>>>(DeleteRecording));
            Main.Instance.AddEventHandler("RecM:getRecordings:Server", new Func<Player, Task<Dictionary<string, Vector4>>>(GetRecordings));
            Main.Instance.AddEventHandler("RecM:openMenu:Server", new Func<Player, Task<bool>>(OpenMenu));

            // Only at startup
            CleanRecordings();
        }

#endif

        #endregion

        #region Events

#if CLIENT

        #region Register recording

        private void RegisterRecording(string name, string cacheString) => API.RegisterStreamingFileFromCache("RecM_records", name, cacheString);

        #endregion

#endif

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
                    posElement.SetAttribute("x", recording.Position.X.ToString("G"));
                    posElement.SetAttribute("y", recording.Position.Y.ToString("G"));
                    posElement.SetAttribute("z", recording.Position.Z.ToString("G"));
                    itemElement.AppendChild(posElement);

                    // Velocity
                    XmlElement velElement = doc.CreateElement("Velocity");
                    velElement.SetAttribute("x", recording.Velocity.X.ToString("G"));
                    velElement.SetAttribute("y", recording.Velocity.Y.ToString("G"));
                    velElement.SetAttribute("z", recording.Velocity.Z.ToString("G"));
                    itemElement.AppendChild(velElement);

                    // Top/Forward
                    XmlElement topElement = doc.CreateElement("Forward");
                    topElement.SetAttribute("x", recording.Forward.X.ToString("G"));
                    topElement.SetAttribute("y", recording.Forward.Y.ToString("G"));
                    topElement.SetAttribute("z", recording.Forward.Z.ToString("G"));
                    itemElement.AppendChild(topElement);

                    // Right
                    XmlElement rightElement = doc.CreateElement("Right");
                    rightElement.SetAttribute("x", recording.Right.X.ToString("G"));
                    rightElement.SetAttribute("y", recording.Right.Y.ToString("G"));
                    rightElement.SetAttribute("z", recording.Right.Z.ToString("G"));
                    itemElement.AppendChild(rightElement);

                    // Steering
                    XmlElement steerElement = doc.CreateElement("Steering");
                    steerElement.SetAttribute("value", recording.SteeringAngle.ToString("G"));
                    itemElement.AppendChild(steerElement);

                    // Gas
                    XmlElement gasElement = doc.CreateElement("GasPedal");
                    gasElement.SetAttribute("value", recording.Gas.ToString("G"));
                    itemElement.AppendChild(gasElement);

                    // Brake
                    XmlElement brakeElement = doc.CreateElement("BrakePedal");
                    brakeElement.SetAttribute("value", recording.Brake.ToString("G"));
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
                {
                    File.WriteAllBytes(Path.Combine(recordingsPath, $"{name}_{model}_001.yvr"), yvrData);
                    var cacheString = API.RegisterResourceAsset("RecM_records", $"stream/{name}_{model}_001.yvr");
                    EventDispatcher.Send(Main.Instance.Clients, "RecM:registerRecording:Client", $"{name}_{model}_001.yvr", cacheString);
                }
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
                        var cacheString = API.RegisterResourceAsset("RecM_records", $"stream/{name}_{model}_{(maxValue + 1).ToString().PadLeft(3, '0')}.yvr");
                        EventDispatcher.Send(Main.Instance.Clients, "RecM:registerRecording:Client", $"{name}_{model}_{(maxValue + 1).ToString().PadLeft(3, '0')}.yvr", cacheString);
                    }
                    else
                    {
                        cb(false, "A recording with this name and model already exists.");
                        return;
                    }
                }

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

        private async Task<Dictionary<string, Vector4>> GetRecordings([FromSource] Player source)
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
                if (!Directory.Exists(recordingsPath))
                    Directory.CreateDirectory(recordingsPath);

                // For custom recordings, find all files that start with the name and model
                Dictionary<string, Vector4> recordings = [];
                foreach (var file in Directory.EnumerateFiles(recordingsPath))
                {
                    // Just to be safe
                    if (!Path.GetFileNameWithoutExtension(file).Contains("_"))
                        continue;

                    var name = Path.GetFileNameWithoutExtension(file).Split('_')[0];
                    var model = Path.GetFileNameWithoutExtension(file).Split('_')[1];
                    var id = Path.GetFileNameWithoutExtension(file).Split('_')[2];
                    var maxValue = Directory.EnumerateFiles(recordingsPath)
                        .Where(x => Path.GetFileNameWithoutExtension(x).StartsWith($"{name}_{model}"))
                        .Select(x => int.Parse(Path.GetFileNameWithoutExtension(x).Split('_')[2]))
                        .Max();

                    // Get the recording's start position
                    var yvr = RpfFile.GetResourceFile<YvrFile>(File.ReadAllBytes(file));
                    var yvrData = yvr.Save();
                    RpfFile.LoadResourceFile(yvr, yvrData, 1);
                    var xml = YvrXml.GetXml(yvr);

                    // This will be used to load/save meta files
                    XmlDocument doc = new();

                    // Load the xml
                    doc.LoadXml(xml);

                    // Grab the position from the xml
                    XmlElement posAttr = doc["VehicleRecordList"].ChildNodes[0]["Position"];
                    var posX = float.Parse(posAttr.GetAttribute("x"));
                    var posY = float.Parse(posAttr.GetAttribute("y"));
                    var posZ = float.Parse(posAttr.GetAttribute("z"));
                    var pos = new Vector3(posX, posY, posZ);

                    // Calculate the heading from the forward vector
                    XmlElement forAttr = doc["VehicleRecordList"].ChildNodes[0]["Forward"];
                    var forX = float.Parse(forAttr.GetAttribute("x"));
                    var forY = float.Parse(forAttr.GetAttribute("y"));
                    var forZ = float.Parse(forAttr.GetAttribute("z"));
                    var heading = (-GameMath.DirectionToHeading(new Vector3(forX, forY, forZ)) + 360) % 360;

                    // Disregard the duplicates that are below the highest id
                    if (id == maxValue.ToString().PadLeft(3, '0'))
                        recordings.Add(Path.GetFileNameWithoutExtension(file), new Vector4(pos, heading));
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

        #region Open menu

        private async Task<bool> OpenMenu([FromSource] Player source)
        {
            if (!API.IsPlayerAceAllowed(source.Handle, $"RecM.openMenu"))
                return false;

            return true;
        }

        #endregion

#endif

        #endregion

        #region Ticks

#if CLIENT

        #region General thread

        private async Task GeneralThread()
        {
            if (ScaleformUI.MenuHandler.IsAnyMenuOpen)
            {
                // I don't want the menu to disable all controls, but I want these ones disabled
                if (!_cinematicCamBlocked)
                {
                    _cinematicCamBlocked = true;
                    API.SetCinematicButtonActive(false);
                }
                Game.DisableControlThisFrame((int)InputMode.GamePad, Control.MeleeAttackLight);
            }
            else
            {
                // Reset it just in case the client likes it this way
                if (_cinematicCamBlocked)
                {
                    _cinematicCamBlocked = false;
                    API.SetCinematicButtonActive(true);
                }
            }
        }

        #endregion

        #region Recording checker thread

        private static async Task RecordingCheckerThread()
        {
            if (!Game.PlayerPed.IsInVehicle()) return;
            if (Game.PlayerPed.CurrentVehicle == null) return;
            if (!Game.PlayerPed.CurrentVehicle.Exists()) return;
            if (Game.PlayerPed.CurrentVehicle.GetPedOnSeat(VehicleSeat.Driver) != Game.PlayerPed) return;
            var veh = Game.PlayerPed.CurrentVehicle;

            // Play the recording until it's done
            if (API.IsPlaybackGoingOnForVehicle(veh.Handle))
            {
                var curr = TimeSpan.FromMilliseconds(_currRecordingPlaybackStartTime - Game.GameTime).ToString(@"mm\:ss");
                var dur = TimeSpan.FromMilliseconds(_currRecordingDuration).ToString(@"mm\:ss");
                Screen.ShowSubtitle($"{curr} / {dur}", 0);
                for (int i = 0; i < _currRecordingPositions.Count; i++)
                {
                    Vector3 recPos = _currRecordingPositions[i];
                    if (i < _currRecordingPositions.Count - 1)
                        World.DrawLine(recPos, _currRecordingPositions[i + 1], Color.FromArgb(255, 0, 0));
                }

                // Stop the player from trying to take control of the steering wheel (thanks Lucas7yoshi!)
                API.SetPlayerControl(Game.Player.Handle, false, 260);

                // Disable vehicle exit
                Game.DisableControlThisFrame(0, Control.VehicleExit);
                if (Game.IsDisabledControlJustReleased(0, Control.VehicleExit))
                    "~r~You can't exit your vehicle whilst recording!".Help(5000);
            }
            else
            {
                // No playback is active and since last recording played is not null, we can assume that the recording is done
                if (_lastRecordingPlayed != null)
                    StopRecordingPlayback();
            }
        }

        #endregion

        #region Recording playback thread

        private static async Task RecordingPlaybackThread()
        {
            RecordThisFrame();
            await BaseScript.Delay(100);
        }

        #endregion

        #region Recording playback checker thread

        private static async Task RecordingPlaybackCheckerThread()
        {
            // Disable vehicle exit
            Game.DisableControlThisFrame(0, Control.VehicleExit);
            if (Game.IsDisabledControlJustReleased(0, Control.VehicleExit))
                "~r~You can't exit your vehicle whilst recording!".Help(5000);
        }

        #endregion

#endif

        #endregion

        #region Tools

#if CLIENT

        #region Start recording

        public static void StartRecording()
        {
            IsRecording = true; 
            Main.Instance.AttachTick(RecordingPlaybackThread);
            Main.Instance.AttachTick(RecordingPlaybackCheckerThread);
        }

        #endregion

        #region Stop recording

        public static void StopRecording()
        {
            IsRecording = false;
            Main.Instance.DetachTick(RecordingPlaybackThread);
            Main.Instance.DetachTick(RecordingPlaybackCheckerThread);
        }

        #endregion

        #region Discard recording

        public static void DiscardRecording()
        {
            IsRecording = false;
            _currRecording.Clear();
            _recordingStartTime = 0;
        }

        #endregion

        #region Save recording

        public static async Task<bool> SaveRecording(string name, bool overwrite = false)
        {
            if (_currRecording.Count == 0)
            {
                "You need to record something first!".Error(true);
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
                    msg.Error(true);
                    tcs.SetResult(false);
                    return;
                }

                // Notify the client of the recording being saved
                "Recording saved!".Log(true);

                // Reset the recording data
                IsRecording = false;
                _currRecording.Clear();
                _recordingStartTime = 0;

                tcs.SetResult(true);
            }));

            return await tcs.Task;
        }

        #endregion

        #region Record this frame

        public static void RecordThisFrame()
        {
            if (!Game.PlayerPed.IsInVehicle()) return;
            if (Game.PlayerPed.CurrentVehicle == null) return;
            if (!Game.PlayerPed.CurrentVehicle.Exists()) return;
            if (Game.PlayerPed.CurrentVehicle.GetPedOnSeat(VehicleSeat.Driver) != Game.PlayerPed) return;
            var veh = Game.PlayerPed.CurrentVehicle;

            // We only need this once per recording, it's the first frame
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

        #region Get recordings

        public static async Task<Tuple<List<string>, Dictionary<string, Vector4>>> GetRecordings()
        {
            // Declare the list that will hold the recordings
            List<string> vanilla = [];

            // Now let's load the vanilla recordings
            var json = API.LoadResourceFile("RecM_records", "vanilla.json");
            if (string.IsNullOrEmpty(json))
                vanilla = Client.data.yvrs.backupVanillaYvrs;
            else
            {
                foreach (var item in Json.Parse<JArray>(json))
                    vanilla.Add(item.ToString());
            }

            // Get the list of custom recordings
            var custom = await EventDispatcher.Get<Dictionary<string, Vector4>>("RecM:getRecordings:Server");

            return new Tuple<List<string>, Dictionary<string, Vector4>>(vanilla, custom);
        }

        #endregion

        #region Play recording

        public async static void PlayRecording(int id, string name, string model = null, Vector4? pos = null)
        {
            try
            {
                if (_recordingCooldown)
                {
                    "Just a 1 second cooldown, please wait...".Warning(true);
                    return;
                }

                // Just an indicator that the recording is playing
                IsLoadingRecording = true;

                Cooldown();

                // Whether the playback is switching to another playback
                bool isSwitching = false;

                Vehicle veh;
                if (model != null)
                {
                    // Get the current vehicle for a backup if it exists
                    Vehicle backupVeh = null;
                    if (Game.PlayerPed.IsInVehicle() && Game.PlayerPed.CurrentVehicle != null)
                    {
                        backupVeh = Game.PlayerPed.CurrentVehicle;

                        // Stop the playback if it's going on
                        if (API.IsPlaybackGoingOnForVehicle(backupVeh.Handle))
                            isSwitching = true;
                        else
                            _lastVehicleModel = backupVeh.DisplayName;
                    }

                    // Check if the model exists
                    if (API.IsModelInCdimage(Game.GenerateHashASCII(model)))
                    {
                        if (backupVeh != null)
                        {
                            // Basically checking if the player's already using the vehicle required for the recording, if so, just use that
                            if (backupVeh.DisplayName == model)
                                veh = backupVeh;
                            else
                                veh = await Tools.SpawnVehicle(model, false);
                        }
                        else
                            veh = await Tools.SpawnVehicle(model, false);
                    }
                    else
                    {
                        // Since the model doesn't exist, we'll use the backup vehicle if it exists, otherwise we'll use the default vehicle
                        if (backupVeh != null)
                        {
                            $"The model {model} for this recording doesn't exist, using your current vehicle instead...".Error(true);
                            veh = backupVeh;
                        }
                        else
                        {
                            $"The model {model} for this recording doesn't exist, using the default vehicle instead...".Error(true);
                            veh = await Tools.SpawnVehicle(_defaultVehicle, false);
                        }
                    }
                }
                else
                {
                    if (Game.PlayerPed.IsInVehicle() && Game.PlayerPed.CurrentVehicle != null)
                    {
                        veh = Game.PlayerPed.CurrentVehicle;

                        // Stop the playback if it's going on
                        if (API.IsPlaybackGoingOnForVehicle(veh.Handle))
                            isSwitching = true;
                        else
                            _lastVehicleModel = veh.DisplayName;
                    }
                    else
                        veh = await Tools.SpawnVehicle(_defaultVehicle, false);
                }

                // This actually should be rare but just in case
                if (veh == null)
                {
                    "The vehicle failed to spawn for the recording.".Error(true);
                    IsLoadingRecording = false;
                    return;
                }

                // Switch the playback if there's already one going on
                if (isSwitching)
                    SwitchRecordingPlayback();

                // Attach the recording checker tick
                Main.Instance.AttachTick(RecordingCheckerThread);

                // Load the recording with a function call, because the FiveM native doesn't take the name parameter
                API.RequestVehicleRecording(id, name);
                var currTime = Main.GameTime;
                while (!Function.Call<bool>(Hash.HAS_VEHICLE_RECORDING_BEEN_LOADED, id, name) && Main.GameTime - currTime < 7000) // With a timeout of 7 seconds
                    await BaseScript.Delay(1000);

                // It might not have loaded, so let's stop here
                if (!Function.Call<bool>(Hash.HAS_VEHICLE_RECORDING_BEEN_LOADED, id, name))
                {
                    IsLoadingRecording = false;
                    "The recording failed to load.".Error(true);
                    return;
                }

                // Save the player's last position only if the last recording has stopped playing or the last location hasn't been stored
                if (!API.IsPlaybackGoingOnForVehicle(veh.Handle) || _lastLocation == null)
                {
                    // If switching to another playback, we don't want this resetting mid sequence
                    if (!isSwitching)
                        _lastLocation = new Vector4(veh.Position, veh.Heading);
                }

                // Now, teleport the player ONLY if there's given coords (which is mostly likely from the custom recordings)
                if (pos != null)
                    await Tools.Teleport((Vector3)pos, pos.Value.W, false);

                // Play the recording
                API.StartPlaybackRecordedVehicle(veh.Handle, id, name, true);

                // I have no idea what it does, but it's in that other yvr recorder script
                API.SetVehicleActiveDuringPlayback(veh.Handle, true);

                // Store it as our last used recording
                _lastRecordingPlayed = new Tuple<int, string>(id, name);

                // The start time of the playback
                _currRecordingPlaybackStartTime = Game.GameTime;

                // The total duration of the recording
                _currRecordingDuration = (int)Function.Call<float>(Hash.GET_TOTAL_DURATION_OF_VEHICLE_RECORDING, id, name);

                // Store the positions every 120ms of the recording
                for (float time = 0; time <= _currRecordingDuration; time += 120)
                {
                    Vector3 position = API.GetPositionOfVehicleRecordingAtTime(id, time, name);
                    _currRecordingPositions.Add(position);
                }

                // Reset things
                IsLoadingRecording = false;
            }
            catch (Exception ex)
            {
                Clean();
                $"The recording failed to load, check the f8 console for the exception.".Error(true);
                ex.ToString().Error();
            }
        }

        #endregion

        #region Switch recording playback

        public static void SwitchRecordingPlayback()
        {
            if (!Game.PlayerPed.IsInVehicle()) return;
            if (Game.PlayerPed.CurrentVehicle == null) return;
            if (!Game.PlayerPed.CurrentVehicle.Exists()) return;
            if (Game.PlayerPed.CurrentVehicle.GetPedOnSeat(VehicleSeat.Driver) != Game.PlayerPed) return;
            var veh = Game.PlayerPed.CurrentVehicle;

            // Remove the recording from memory
            if (_lastRecordingPlayed != null)
                API.RemoveVehicleRecording(_lastRecordingPlayed.Item1, _lastRecordingPlayed.Item2);

            // Reset things
            _lastRecordingPlayed = null;
            _currRecordingPositions.Clear();

            // Stop the playback
            if (API.IsPlaybackGoingOnForVehicle(veh.Handle))
                API.StopPlaybackRecordedVehicle(veh.Handle);
        }

        #endregion

        #region Delete recording

        public static async Task<bool> DeleteRecording(string name, string model)
        {
            // Latent event that sends little increments of data to the server
            (bool success, string msg) = await EventDispatcher.Get<Tuple<bool, string>>("RecM:deleteRecording:Server", name, model);
            if (!success)
            {
                msg.Error(true);
                return false;
            }

            // Notify the client of the recording being deleted
            $"Recording {name} successfully deleted!".Log(true);

            return true;
        }

        #endregion

        #region Stop recording playback

        public async static void StopRecordingPlayback()
        {
            if (_lastRecordingPlayed == null)
            {
                "There's no recording being played at this moment.".Error(true);
                return;
            }
            if (!Game.PlayerPed.IsInVehicle()) return;
            if (Game.PlayerPed.CurrentVehicle == null) return;
            if (!Game.PlayerPed.CurrentVehicle.Exists()) return;
            if (Game.PlayerPed.CurrentVehicle.GetPedOnSeat(VehicleSeat.Driver) != Game.PlayerPed) return;
            var veh = Game.PlayerPed.CurrentVehicle;

            // Remove the recording from memory
            if (_lastRecordingPlayed != null)
                API.RemoveVehicleRecording(_lastRecordingPlayed.Item1, _lastRecordingPlayed.Item2);

            // Reset things
            _lastRecordingPlayed = null;
            _currRecordingPositions.Clear();

            // Stop the playback
            API.StopPlaybackRecordedVehicle(veh.Handle);

            // Detach the tick
            Main.Instance.DetachTick(RecordingCheckerThread);

            // Attempt to spawn the player's last vehicle
            if (_lastVehicleModel != null && _lastVehicleModel != veh.DisplayName)
                await Tools.SpawnVehicle(_lastVehicleModel, true);

            // Teleport the player back to the last location
            if (_lastLocation != null)
                await Tools.Teleport((Vector3)_lastLocation, _lastLocation.Value.W, false);

            // Reset player control flags
            API.SetPlayerControl(Game.Player.Handle, true, 0);

            // Conditional reset things
            _lastLocation = null;
            _lastVehicleModel = null;

            "Recording stopped!".Log(true);
        }

        #endregion

        #region Clean

        private static void Clean()
        {
            if (!Game.PlayerPed.IsInVehicle()) return;
            if (Game.PlayerPed.CurrentVehicle == null) return;
            if (!Game.PlayerPed.CurrentVehicle.Exists()) return;
            if (Game.PlayerPed.CurrentVehicle.GetPedOnSeat(VehicleSeat.Driver) != Game.PlayerPed) return;
            var veh = Game.PlayerPed.CurrentVehicle;

            if (_lastRecordingPlayed != null)
                API.RemoveVehicleRecording(_lastRecordingPlayed.Item1, _lastRecordingPlayed.Item2);
            if (!API.IsPlaybackGoingOnForVehicle(veh.Handle))
            {
                _lastLocation = null;
                _lastVehicleModel = null;
            }
            _lastRecordingPlayed = null;
            _currRecordingPositions.Clear();
        }

        #endregion

        #region Cooldown

        private static async void Cooldown()
        {
            _recordingCooldown = true;
            await BaseScript.Delay(1000);
            _recordingCooldown = false;
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
                    // Just to be safe
                    if (!Path.GetFileNameWithoutExtension(file).Contains("_"))
                        continue;

                    var name = Path.GetFileNameWithoutExtension(file).Split('_')[0];
                    var model = Path.GetFileNameWithoutExtension(file).Split('_')[1];
                    var id = Path.GetFileNameWithoutExtension(file).Split('_')[2];
                    var maxValue = Directory.EnumerateFiles(recordingsPath)
                        .Where(x => Path.GetFileNameWithoutExtension(x).StartsWith($"{name}_{model}"))
                        .Select(x => int.Parse(Path.GetFileNameWithoutExtension(x).Split('_')[2]))
                        .DefaultIfEmpty(0)
                        .Max();

                    // Delete the file if there's a duplicate with a lower recording id
                    if (id != maxValue.ToString().PadLeft(3, '0'))
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

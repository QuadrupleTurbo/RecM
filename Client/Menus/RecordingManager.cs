using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using FxEvents;
using FxEvents.Shared.TypeExtensions;
using RecM.Client.Utils;
using ScaleformUI;
using ScaleformUI.Menu;
using ScaleformUI.Scaleforms;

namespace RecM.Client.Menus
{
    public class RecordingManager
    {
        #region Fields

        private static UIMenu menu;
        private static UIMenuItem _stopRecordingMenuItem;
        private static UIMenuItem _startRecordingMenuItem;
        private static UIMenuItem _discardRecordingMenuItem;
        private static UIMenuItem _saveRecordingMenuItem;
        private static UIMenuItem _createRecordingsMenuItem;
        private static List<string> _lastVanillaRecordings = null;
        private static Dictionary<string, Vector4> _lastCustomRecordings = null;
        private static int _lastVanillaRecordingsMenuIndex;
        private static int _lastCustomRecordingsMenuIndex;

        #endregion

        #region Constructor

        public RecordingManager()
        {
            Main.Instance.RegisterKeyMapping("recm_menu", "Vehicle Recording Utility.", "F7", new Action<int, List<object>, string>(async (source, args, rawCommand) =>
            {
                bool success = await EventDispatcher.Get<bool>("RecM:openMenu:Server");
                if (success && !MenuHandler.IsAnyMenuOpen)
                    menu.Visible = true;
            }));

            CreateMenu();
        }

        #endregion

        #region Tools

        #region Create menu

        public async static void CreateMenu()
        {
            if (MenuHandler.IsAnyMenuOpen) return;

            menu = new UIMenu("RecM", "Vehicle Recording Utility", new PointF(960, 20), "recm_textures", "recm_banner", true);
            menu.ControlDisablingEnabled = false;
            menu.EnableAnimation = false;
            menu.BuildingAnimation = MenuBuildingAnimation.NONE;
            menu.Enabled3DAnimations = false;

            #region Create recordings

            _createRecordingsMenuItem = new UIMenuItem("Create Recording", "Create your own recordings which will save to your Saved Recordings menu.");
            _createRecordingsMenuItem.SetRightLabel("→→→");
            menu.AddItem(_createRecordingsMenuItem);
            UIMenu createRecordingsMenu = new UIMenu("Create Recording", "Create Recording");
            createRecordingsMenu.ControlDisablingEnabled = false;
            _createRecordingsMenuItem.Activated += (sender, e) =>
            {
                sender.SwitchTo(createRecordingsMenu, inheritOldMenuParams: true);
            };

            _startRecordingMenuItem = new UIMenuItem("Start Recording", "Start recording the vehicle's data.");
            createRecordingsMenu.AddItem(_startRecordingMenuItem);
            _startRecordingMenuItem.Activated += async (sender, e) =>
            {
                // This is gonna be behind a locked item anyways
                if (Recording.IsRecording)
                {
                    "There's a recording being made at the moment, please wait...".Alert(true);
                    return;
                }
                if (!Game.PlayerPed.IsInVehicle())
                {
                    "You need to be in a vehicle to start recording.".Alert(true);
                    return;
                }
                if (Game.PlayerPed.CurrentVehicle == null)
                {
                    "Your vehicle is null, you can't record with this.".Alert(true);
                    return;
                }
                if (!Game.PlayerPed.CurrentVehicle.Exists())
                {
                    "Your vehicle doesn't exist, you can't record like this.".Alert(true);
                    return;
                }
                if (Game.PlayerPed.CurrentVehicle.GetPedOnSeat(VehicleSeat.Driver) != Game.PlayerPed)
                {
                    "You aren't the driver, you need to be to record with this vehicle.".Alert(true);
                    return;
                }

                _startRecordingMenuItem.Enabled = false;
                _startRecordingMenuItem.Description = "Recording...";
                _stopRecordingMenuItem.Enabled = true;
                Recording.StartRecording();
            };

            _stopRecordingMenuItem = new UIMenuItem("Stop Recording", "Stop recording the vehicle's data.") { Enabled = false };
            createRecordingsMenu.AddItem(_stopRecordingMenuItem);
            _stopRecordingMenuItem.Activated += async (sender, e) =>
            {
                // This is gonna be behind a locked item anyways
                if (!Recording.IsRecording)
                {
                    "There's nothing being recorded at the moment".Alert(true);
                    return;
                }

                _startRecordingMenuItem.Description = "Save or discard your recording.";
                _stopRecordingMenuItem.Enabled = false;
                _stopRecordingMenuItem.Description = "Save or discard your recording.";
                _discardRecordingMenuItem.Enabled = true;
                _saveRecordingMenuItem.Enabled = true;
                Recording.StopRecording();
            };

            _discardRecordingMenuItem = new UIMenuItem("~r~Discard Recording", "Discard the recording you've just recorded.") { Enabled = false };
            createRecordingsMenu.AddItem(_discardRecordingMenuItem);
            _discardRecordingMenuItem.Activated += async (sender, e) =>
            {
                _startRecordingMenuItem.Enabled = true;
                _startRecordingMenuItem.Description = "Start recording the vehicle's data.";
                _stopRecordingMenuItem.Description = "Stop recording the vehicle's data.";
                _discardRecordingMenuItem.Enabled = false;
                _saveRecordingMenuItem.Enabled = false;
                Recording.DiscardRecording();
            };

            _saveRecordingMenuItem = new UIMenuItem("~g~Save Recording", "Save the recording to your Saved Recordings menu.") { Enabled = false };
            createRecordingsMenu.AddItem(_saveRecordingMenuItem);
            _saveRecordingMenuItem.Activated += async (sender, e) =>
            {
                _saveRecordingMenuItem.Enabled = false;
                _discardRecordingMenuItem.Enabled = false;

                var ui = await Tools.GetUserInput("Enter a name for your recording", 30);
                if (!string.IsNullOrEmpty(ui))
                {
                    // Join the words together since we can't have spaces in the name
                    ui = ui.Replace(" ", "");
                    var success = await Recording.SaveRecording(ui);
                    if (success)
                    {
                        _startRecordingMenuItem.Enabled = true;
                        _startRecordingMenuItem.Description = "Start recording the vehicle's data.";
                        _stopRecordingMenuItem.Description = "Stop recording the vehicle's data.";
                        _discardRecordingMenuItem.Enabled = false;
                        _saveRecordingMenuItem.Enabled = false;
                        sender.RefreshMenu();
                    }
                    else
                    {
                        _saveRecordingMenuItem.Enabled = true;
                        _discardRecordingMenuItem.Enabled = true;
                    }
                }
            };

            #endregion

            #region Saved recordings

            UIMenuItem savedRecordingsMenuItem = new UIMenuItem("Saved Recordings", "This menu contains all the saved recordings.");
            savedRecordingsMenuItem.SetRightLabel("→→→");
            menu.AddItem(savedRecordingsMenuItem);
            UIMenu savedRecordingsMenu = new UIMenu("Saved Recordings", "All Saved Recordings");
            savedRecordingsMenu.ControlDisablingEnabled = false;
            savedRecordingsMenuItem.Activated += (sender, e) =>
            {
                sender.SwitchTo(savedRecordingsMenu, inheritOldMenuParams: true);
            };

            UIMenuItem vanillaRecordingsMenuItem = new UIMenuItem("Vanilla", "This menu contains all the vanilla recording data.");
            vanillaRecordingsMenuItem.SetRightLabel("→→→");
            savedRecordingsMenu.AddItem(vanillaRecordingsMenuItem);
            UIMenu vanillaRecordingsMenu = new UIMenu("Vanilla", "All Vanilla Recordings");
            vanillaRecordingsMenu.ControlDisablingEnabled = false;
            vanillaRecordingsMenuItem.Activated += (sender, e) =>
            {
                sender.SwitchTo(vanillaRecordingsMenu, inheritOldMenuParams: true, newMenuCurrentSelection: _lastVanillaRecordingsMenuIndex);
            };

            UIMenuItem customRecordingsMenuItem = new UIMenuItem("Custom", "This menu contains all the custom recording data.");
            customRecordingsMenuItem.SetRightLabel("→→→");
            savedRecordingsMenu.AddItem(customRecordingsMenuItem);
            UIMenu customRecordingsMenu = new UIMenu("Custom", "All Custom Recordings");
            customRecordingsMenu.ControlDisablingEnabled = false;
            customRecordingsMenuItem.Activated += (sender, e) =>
            {
                sender.SwitchTo(customRecordingsMenu, inheritOldMenuParams: true, newMenuCurrentSelection: _lastCustomRecordingsMenuIndex);
            };

            menu.OnMenuOpen += async (menu, data) =>
            {
                savedRecordingsMenuItem.Enabled = false;
                savedRecordingsMenuItem.Description = "Loading...";

                // Get the recordings
                (List<string> vanilla, Dictionary<string, Vector4> custom) = await Recording.GetRecordings();

                savedRecordingsMenuItem.Enabled = true;
                savedRecordingsMenuItem.Description = "This menu contains all the saved recordings.";

                #region Vanilla recordings

                if (_lastVanillaRecordings == null || !_lastVanillaRecordings.SequenceEqual(vanilla))
                {
                    _lastVanillaRecordingsMenuIndex = 0;
                    _lastVanillaRecordings = vanilla;
                    vanillaRecordingsMenu.Clear();
                    vanillaRecordingsMenu.InstructionalButtons.RemoveAll(x => !x.Text.Equals("Back") && !x.Text.Equals("Select"));
                    if (vanilla.Count > 0)
                    {
                        vanillaRecordingsMenuItem.Enabled = true;
                        vanillaRecordingsMenuItem.Description = "This menu contains all the vanilla recording data.";
                        vanillaRecordingsMenuItem.SetRightBadge(BadgeIcon.NONE);

                        Dictionary<string, List<string>> vanillaRecordings = [];
                        foreach (var recording in vanilla)
                        {
                            string name = recording.Substring(0, recording.Length - 3);
                            string id = recording.Substring(recording.Length - 3);

                            if (!vanillaRecordings.ContainsKey(name))
                                vanillaRecordings.Add(name, [id]);
                            else
                                vanillaRecordings[name].Add(id);
                        }

                        var stopRecordingBtn = new InstructionalButton(Control.Jump, Control.Jump, "Stop Playback");
                        vanillaRecordingsMenu.InstructionalButtons.Add(stopRecordingBtn);
                        stopRecordingBtn.OnControlSelected += (_) =>
                        {
                            Recording.StopRecordingPlayback();
                        };

                        var filterBtn = new InstructionalButton(Control.Duck, Control.Duck, "Filter");
                        vanillaRecordingsMenu.InstructionalButtons.Add(filterBtn);
                        filterBtn.OnControlSelected += async (_) =>
                        {
                            "This feature is currently disabled due to a flaw in the menu API.".Alert(true);
                            /*string filter = await Tools.GetUserInput("Enter a word (leave blank to reset)", 10);
                            if (string.IsNullOrEmpty(filter))
                            {
                                // Only real way to tell with the api whether the menu is filtered or not
                                if (vanillaRecordingsMenu._unfilteredMenuItems.Count > 0)
                                    vanillaRecordingsMenu.ResetFilter();
                                return;
                            }

                            vanillaRecordingsMenu.FilterMenuItems((mb) => mb.Label.ToLower().Contains(filter.ToLower()));*/
                        };

                        foreach (var recording in vanillaRecordings)
                        {
                            if (!vanillaRecordingsMenu.MenuItems.Any(x => x.Label.Equals(recording.Key)))
                            {
                                var listItem = new UIMenuListItem(recording.Key, [], 0);
                                vanillaRecordingsMenu.AddItem(listItem);
                                listItem.ItemData = recording.Value;
                                foreach (var id in recording.Value)
                                    listItem.Items.Add(id);

                                // Reorder the items by ID from lowest to highest
                                listItem.Items = listItem.Items.OrderBy(x => x).ToList();

                                listItem.OnListSelected += (item, index) =>
                                {
                                    if (Recording.IsLoadingRecording)
                                    {
                                        "There's a recording being loaded at the moment, please wait...".Alert(true);
                                        return;
                                    }

                                    Recording.PlayRecording(int.Parse(item.Items[index].ToString()), item.Label);
                                };
                            }
                            else
                            {
                                var listItem = vanillaRecordingsMenu.MenuItems.FirstOrDefault(x => x.Label.Equals(recording.Key)) as UIMenuListItem;
                                foreach (var id in recording.Value)
                                    listItem.Items.Add(id);

                                // Reorder the items by ID from lowest to highest
                                listItem.Items = listItem.Items.OrderBy(x => x).ToList();
                            }
                        }
                    }
                    else
                    {
                        vanillaRecordingsMenuItem.Enabled = false;
                        vanillaRecordingsMenuItem.Description = "This menu contains no vanilla recordings.";
                        vanillaRecordingsMenuItem.SetRightBadge(BadgeIcon.LOCK);
                    }
                }

                #endregion

                #region Custom recordings

                if (_lastCustomRecordings == null || !_lastCustomRecordings.SequenceEqual(custom))
                {
                    _lastCustomRecordingsMenuIndex = 0;
                    _lastCustomRecordings = custom;
                    customRecordingsMenu.Clear();
                    if (custom.Count > 0)
                    {
                        customRecordingsMenuItem.Enabled = true;
                        customRecordingsMenuItem.Description = "This menu contains all the custom recording data.";
                        customRecordingsMenuItem.SetRightBadge(BadgeIcon.NONE);

                        foreach (var recording in custom)
                        {
                            var name = recording.Key.Split('_')[0];
                            var model = recording.Key.Split('_')[1];
                            var id = int.Parse(recording.Key.Split('_')[2]);
                            var pos = recording.Value;

                            UIMenuItem recordItem = new UIMenuItem(name, $"Vehicle: {model}\nID: {id}");
                            recordItem.ItemData = recording;
                            recordItem.SetRightLabel("→→→");
                            customRecordingsMenu.AddItem(recordItem);
                            UIMenu recordItemMenu = new UIMenu(name, name);
                            recordItemMenu.ControlDisablingEnabled = false;
                            recordItem.Activated += (sender, e) =>
                            {
                                sender.SwitchTo(recordItemMenu, inheritOldMenuParams: true);
                            };

                            UIMenuItem playItem = new UIMenuItem("Play", "Play the recording.");
                            recordItemMenu.AddItem(playItem);
                            playItem.Activated += async (sender, e) =>
                            {
                                Recording.PlayRecording(id, $"{name}_{model}_", model, pos);
                            };

                            UIMenuItem stopItem = new UIMenuItem("Stop", "Stop the recording.");
                            recordItemMenu.AddItem(stopItem);
                            stopItem.Activated += (sender, e) =>
                            {
                                Recording.StopRecordingPlayback();
                            };

                            UIMenuItem deleteItem = new UIMenuItem("~r~Delete", "Delete the recording.");
                            recordItemMenu.AddItem(deleteItem);
                            deleteItem.Activated += async (sender, e) =>
                            {
                                var success = await Recording.DeleteRecording(name, model);
                                if (success)
                                {
                                    sender.GoBack();
                                    customRecordingsMenu.GoBack();
                                    savedRecordingsMenu.GoBack();
                                }
                            };
                        }
                    }
                    else
                    {
                        customRecordingsMenuItem.Enabled = false;
                        customRecordingsMenuItem.Description = "This menu contains no custom recordings.";
                        customRecordingsMenuItem.SetRightBadge(BadgeIcon.LOCK);
                    }
                }

                #endregion
            };

            vanillaRecordingsMenu.OnIndexChange += (menu, index) =>
            {
                _lastVanillaRecordingsMenuIndex = index;
            };

            customRecordingsMenu.OnIndexChange += (menu, index) =>
            {
                _lastCustomRecordingsMenuIndex = index;
            };

            #endregion

            #region Credits

            var creditsMenuItem = new UIMenuItem("Credits", "All of the people that helped with the creation of the script either directly or indirectly.");
            creditsMenuItem.SetRightBadge(BadgeIcon.ROCKSTAR);
            menu.AddItem(creditsMenuItem);
            UIMenu creditsMenu = new UIMenu("Credits", "Credits");
            creditsMenu.ControlDisablingEnabled = false;
            creditsMenuItem.Activated += (sender, e) =>
            {
                sender.SwitchTo(creditsMenu, inheritOldMenuParams: true);
            };

            var dexyfexItem = new UIMenuItem("Dexyfex", "Author of Codewalker, it provided the tools for ovr -> yvr conversion.");
            creditsMenu.AddItem(dexyfexItem);
            dexyfexItem.SetRightLabel("(Click To Visit Repo)");
            dexyfexItem.Activated += async (sender, e) =>
            {
                "The link will now open in your browser.".Warning(true);
                await BaseScript.Delay(3000);
                API.SendNuiMessage(Json.Stringify(new { url = "https://github.com/dexyfex/CodeWalker" }));
            };
            var manups4eItem = new UIMenuItem("Manups4e", "Author of ScaleformUI, this menu's API.");
            creditsMenu.AddItem(manups4eItem);
            manups4eItem.SetRightLabel("(Click To Visit Repo)");
            manups4eItem.Activated += async (sender, e) =>
            {
                "The link will now open in your browser.".Warning(true);
                await BaseScript.Delay(3000);
                API.SendNuiMessage(Json.Stringify(new { url = "https://github.com/manups4e/ScaleformUI" }));
            };
            var lucas7yoshiItem = new UIMenuItem("Lucas7yoshi", "For providing great help and research for the vehicle recordings from within the Codewalker Discord.");
            creditsMenu.AddItem(lucas7yoshiItem);

            #endregion
        }

        #endregion

        #endregion
    }
}

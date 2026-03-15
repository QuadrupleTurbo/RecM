using CitizenFX.Core;
using CitizenFX.Core.Native;
using FxEvents;
using RecM.Client.Utils;
using ScaleformUI;
using ScaleformUI.Menu;
using ScaleformUI.Scaleforms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

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
        private static bool _discardConfirmPending;
        private static InstructionalButton _showMenuBtn;

        #endregion

        #region Constructor

        public RecordingManager()
        {
            EventHub.Send("RecM:syncRecordings:Server");
            Main.Instance.RegisterKeyMapping("recm_menu", "Vehicle Recording Utility.", "F7", new Action<int, List<object>, string>(async (source, args, rawCommand) =>
            {
                if (MenuHandler.IsAnyMenuOpen) return;
                bool success = await EventHub.Get<bool>("RecM:openMenu:Server");
                if (success)
                {
                    if (MenuHandler.IsAnyMenuOpen) return; // Another check since we're waiting for the server to respond
                    menu.Visible = true;
                }
                else
                    "You do not have permission to open the menu.".Alert(true);
            }));

            CreateMenu();
        }

        #endregion

        #region Tools

        #region Show/hide menu button helpers

        private static void AddShowMenuButton(UIMenu targetMenu)
        {
            RemoveShowMenuButton();
            _showMenuBtn = new InstructionalButton(Control.VehicleHorn, "Show Menu");
            _showMenuBtn.OnControlSelected += (_) =>
            {
                RemoveShowMenuButton();
                targetMenu.Visible = true;
            };
            ScaleformUI.Main.InstructionalButtons.AddInstructionalButton(_showMenuBtn);
        }

        private static void RemoveShowMenuButton()
        {
            if (_showMenuBtn == null) return;
            ScaleformUI.Main.InstructionalButtons.RemoveInstructionalButton(_showMenuBtn);
            _showMenuBtn = null;
        }

        #endregion

        #region Create menu

        public async static void CreateMenu()
        {
            if (MenuHandler.IsAnyMenuOpen) return;

            menu = new UIMenu("RecM", "Vehicle Recording Utility", new PointF(960, 20), "recm_textures", "recm_banner", true);
            menu.ControlDisablingEnabled = false;
            menu.MaxItemsOnScreen = 15;

            #region Create recordings

            _createRecordingsMenuItem = new UIMenuItem("Create Recording", "Create your own recordings which will save to your Saved Recordings menu.");
            _createRecordingsMenuItem.SetRightLabel("→→→");
            menu.AddItem(_createRecordingsMenuItem);
            UIMenu createRecordingsMenu = new UIMenu("Create Recording", "Create Recording");
            createRecordingsMenu.ControlDisablingEnabled = false;
            var hideMenuBtnCreate = new InstructionalButton(Control.VehicleHorn, Control.VehicleHorn, "Hide Menu");
            createRecordingsMenu.InstructionalButtons.Add(hideMenuBtnCreate);
            hideMenuBtnCreate.OnControlSelected += (_) =>
            {
                createRecordingsMenu.Visible = false;
                AddShowMenuButton(createRecordingsMenu);
            };
            createRecordingsMenu.OnMenuOpen += (m, d) => RemoveShowMenuButton();
            _createRecordingsMenuItem.Activated += (sender, e) =>
            {
                sender.SwitchTo(createRecordingsMenu, inheritOldMenuParams: true);
            };

            _startRecordingMenuItem = new UIMenuItem("Start Recording", "Start recording the vehicle's data.");
            createRecordingsMenu.AddItem(_startRecordingMenuItem);
            _startRecordingMenuItem.Activated += async (sender, e) =>
            {
                bool success = Recording.StartRecording();
                if (!success) return;
                _startRecordingMenuItem.Enabled = false;
                _startRecordingMenuItem.Description = "Recording...";
                _stopRecordingMenuItem.Enabled = true;
            };

            _stopRecordingMenuItem = new UIMenuItem("Stop Recording", "Stop recording the vehicle's data.") { Enabled = false };
            createRecordingsMenu.AddItem(_stopRecordingMenuItem);
            _stopRecordingMenuItem.Activated += async (sender, e) =>
            {
                var success = Recording.StopRecording();
                if (!success) return;
                _discardConfirmPending = false;
                _startRecordingMenuItem.Description = "Save or discard your recording.";
                _stopRecordingMenuItem.Enabled = false;
                _stopRecordingMenuItem.Description = "Save or discard your recording.";
                _discardRecordingMenuItem.Enabled = true;
                _saveRecordingMenuItem.Enabled = true;
            };

            _discardRecordingMenuItem = new UIMenuItem("~r~Discard Recording", "Discard the recording you've just recorded.") { Enabled = false };
            createRecordingsMenu.AddItem(_discardRecordingMenuItem);
            _discardRecordingMenuItem.Activated += async (sender, e) =>
            {
                if (!_discardConfirmPending)
                {
                    _discardConfirmPending = true;
                    _discardRecordingMenuItem.Label = "~r~Confirm Discard?";
                    _discardRecordingMenuItem.Description = "Press again to confirm discarding the recording.";
                    sender.RefreshMenu(true);
                    return;
                }
                _discardConfirmPending = false;
                _discardRecordingMenuItem.Label = "~r~Discard Recording";
                _discardRecordingMenuItem.Description = "Discard the recording you've just recorded.";
                _startRecordingMenuItem.Enabled = true;
                _startRecordingMenuItem.Description = "Start recording the vehicle's data.";
                _stopRecordingMenuItem.Description = "Stop recording the vehicle's data.";
                _discardRecordingMenuItem.Enabled = false;
                _saveRecordingMenuItem.Enabled = false;
                Recording.DiscardRecording();
                sender.RefreshMenu(false);
            };

            _saveRecordingMenuItem = new UIMenuItem("~g~Save Recording", "Save the recording to your Saved Recordings menu.") { Enabled = false };
            createRecordingsMenu.AddItem(_saveRecordingMenuItem);
            _saveRecordingMenuItem.Activated += async (sender, e) =>
            {
                _saveRecordingMenuItem.Enabled = false;
                _discardRecordingMenuItem.Enabled = false;

                var ui = await Tools.GetUserInput("Enter a name for your recording", 30);
                if (string.IsNullOrEmpty(ui))
                {
                    // User cancelled — restore buttons
                    _saveRecordingMenuItem.Enabled = true;
                    _discardRecordingMenuItem.Enabled = true;
                    return;
                }
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
                        sender.RefreshMenu(false);
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
                sender.SwitchTo(vanillaRecordingsMenu, inheritOldMenuParams: true);
            };

            UIMenuItem customRecordingsMenuItem = new UIMenuItem("Custom", "This menu contains all the custom recording data.");
            customRecordingsMenuItem.SetRightLabel("→→→");
            savedRecordingsMenu.AddItem(customRecordingsMenuItem);
            UIMenu customRecordingsMenu = new UIMenu("Custom", "All Custom Recordings");
            customRecordingsMenu.ControlDisablingEnabled = false;
            customRecordingsMenuItem.Activated += (sender, e) =>
            {
                sender.SwitchTo(customRecordingsMenu, inheritOldMenuParams: true);
            };

            menu.OnMenuOpen += async (menu, data) =>
            {
                savedRecordingsMenuItem.Enabled = false;
                savedRecordingsMenuItem.Description = "Loading...";

                // Get the recordings
                List<string> vanilla = Recording.GetVanillaRecordings();
                Dictionary<string, Vector4> custom = await Recording.GetCustomRecordings();

                savedRecordingsMenuItem.Enabled = true;
                savedRecordingsMenuItem.Description = "This menu contains all the saved recordings.";

                #region Vanilla recordings

                if (_lastVanillaRecordings == null || !_lastVanillaRecordings.SequenceEqual(vanilla))
                {
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

                        var filterBtn = new InstructionalButton(Control.LookBehind, Control.LookBehind, "Filter");
                        vanillaRecordingsMenu.InstructionalButtons.Add(filterBtn);
                        filterBtn.OnControlSelected += async (_) =>
                        {
                            //"This feature is currently disabled due to a flaw in the menu API.".Alert(true);
                            string filter = await Tools.GetUserInput("Enter a word (leave blank to reset)", 20);

                            if (string.IsNullOrEmpty(filter))
                            {
                                // Check if the menu is filtered
                                if (vanillaRecordingsMenu._unfilteredMenuItems.Count > 0)
                                {
                                    "The filter has been reset.".Alert();
                                    vanillaRecordingsMenu.ResetFilter();
                                }

                                return;
                            }

                            // Check if the menu is filtered
                            if (vanillaRecordingsMenu._unfilteredMenuItems.Count > 0)
                            {
                                "The filter has been reset.".Alert();
                                vanillaRecordingsMenu.ResetFilter();
                            }

                            // Filter the menu items
                            vanillaRecordingsMenu.FilterMenuItems((mb) => mb.Label.ToLower().Contains(filter.ToLower()));
                        };

                        var stopRecordingBtn = new InstructionalButton(Control.Jump, Control.Jump, "Stop Playback");
                        vanillaRecordingsMenu.InstructionalButtons.Add(stopRecordingBtn);
                        stopRecordingBtn.OnControlSelected += (_) =>
                        {
                            Recording.StopRecordingPlayback();
                        };

                        var switchPlaybackSpeedNextBtn = new InstructionalButton(Control.FrontendRb, Control.FrontendLs, $"Faster");
                        vanillaRecordingsMenu.InstructionalButtons.Add(switchPlaybackSpeedNextBtn);
                        switchPlaybackSpeedNextBtn.OnControlSelected += (_) =>
                        {
                            Recording.SwitchPlaybackSpeed(Recording.GetPlaybackSpeedIndex() + 1);
                        };

                        var switchPlaybackSpeedPrevBtn = new InstructionalButton(Control.FrontendLb, Control.FrontendRs, $"Slower");
                        vanillaRecordingsMenu.InstructionalButtons.Add(switchPlaybackSpeedPrevBtn);
                        switchPlaybackSpeedPrevBtn.OnControlSelected += (_) =>
                        {
                            Recording.SwitchPlaybackSpeed(Recording.GetPlaybackSpeedIndex() - 1);
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
                                    Recording.StartRecordingPlayback(int.Parse(item.Items[index].ToString()), item.Label);
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

                        vanillaRecordingsMenu.MenuItems.Sort((a, b) => { return a.Label.ToLower().CompareTo(b.Label.ToLower()); });
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

                            var hideMenuBtnPlayback = new InstructionalButton(Control.VehicleHorn, Control.VehicleHorn, "Hide Menu");
                            recordItemMenu.InstructionalButtons.Add(hideMenuBtnPlayback);
                            hideMenuBtnPlayback.OnControlSelected += (_) =>
                            {
                                recordItemMenu.Visible = false;
                                AddShowMenuButton(recordItemMenu);
                            };
                            recordItemMenu.OnMenuOpen += (m, d) => RemoveShowMenuButton();
                            var stopPlaybackBtn = new InstructionalButton(Control.Jump, Control.Jump, "Stop Playback");
                            recordItemMenu.InstructionalButtons.Add(stopPlaybackBtn);
                            stopPlaybackBtn.OnControlSelected += (_) => Recording.StopRecordingPlayback();

                            recordItem.Activated += (sender, e) =>
                            {
                                sender.SwitchTo(recordItemMenu, inheritOldMenuParams: true);

                                // Update the playback speed display (best place to do it)
                                ((UIMenuDynamicListItem)recordItemMenu.MenuItems.FirstOrDefault(x => x.Label.Equals("Playback Speed"))).CurrentListItem = Recording.GetCustomPlaybackSpeedName().Replace("Speed ", "");
                            };

                            UIMenuItem teleportItem = new UIMenuItem("Teleport", "Teleport to where this recording starts.");
                            recordItemMenu.AddItem(teleportItem);
                            teleportItem.Activated += async (sender, e) =>
                            {
                                await Tools.Teleport((Vector3)pos, pos.W, false);
                            };

                            UIMenuListItem playItem = new UIMenuListItem("Play", new List<dynamic> { "Normal", "Chase", "Chase Rubberband" }, 0, "Play the recording. Normal: ride inside the vehicle. Chase: follow in your own vehicle. Chase Rubberband: follow with speed matching.");
                            recordItemMenu.AddItem(playItem);
                            playItem.OnListSelected += async (sender, e) =>
                            {
                                bool chase = playItem.Index == 1;
                                bool chaseRubberband = playItem.Index == 2;
                                Recording.StartRecordingPlayback(id, $"{name}_{model}_", model: model, pos: pos, useMyPlayer: playItem.Index == 0, chaseMode: chase, chaseRubberband: chaseRubberband);
                            };

                            var playbackSpeedItem = new UIMenuDynamicListItem("Playback Speed", "Change the playback speed.", Recording.GetCustomPlaybackSpeedName().Replace("Speed ", ""), async (item, dir) =>
                            {
                                if (dir == ChangeDirection.Left)
                                {
                                    if (Recording.GetCustomPlaybackSpeedIndex() == 0)
                                        return Recording.GetCustomPlaybackSpeedName().Replace("Speed ", "");

                                    Recording.SwitchCustomPlaybackSpeed(Recording.GetCustomPlaybackSpeedIndex() - 1);
                                }
                                else if (dir == ChangeDirection.Right)
                                {
                                    if (Recording.GetCustomPlaybackSpeedIndex() == Recording.GetPlaybackSpeedNameList().Count - 1)
                                        return Recording.GetCustomPlaybackSpeedName().Replace("Speed ", "");

                                    Recording.SwitchCustomPlaybackSpeed(Recording.GetCustomPlaybackSpeedIndex() + 1);
                                }

                                return Recording.GetCustomPlaybackSpeedName().Replace("Speed ", "");
                            });
                            recordItemMenu.AddItem(playbackSpeedItem);

                            Recording.OnPlaybackStateChanged += (playing) =>
                            {
                                playItem.Enabled = !playing;
                                teleportItem.Enabled = !playing;
                                playbackSpeedItem.Enabled = !playing || !Recording.IsRubberbanding;
                            };

                            UIMenuItem stopItem = new UIMenuItem("Stop", "Stop the recording.");
                            recordItemMenu.AddItem(stopItem);
                            stopItem.Activated += (sender, e) =>
                            {
                                Recording.StopRecordingPlayback();
                            };

                            bool deleteConfirmPending = false;
                            UIMenuItem deleteItem = new UIMenuItem("~r~Delete", "Delete the recording.");
                            recordItemMenu.AddItem(deleteItem);
                            deleteItem.Activated += async (sender, e) =>
                            {
                                if (!deleteConfirmPending)
                                {
                                    deleteConfirmPending = true;
                                    deleteItem.Label = "~r~Confirm Delete?";
                                    deleteItem.Description = "Press again to confirm deleting the recording.";
                                    sender.RefreshMenu(true);
                                    return;
                                }
                                deleteConfirmPending = false;
                                deleteItem.Label = "~r~Delete";
                                deleteItem.Description = "Delete the recording.";
                                var success = await Recording.DeleteRecording(name, model);
                                if (success)
                                {
                                    sender.GoBack();
                                    customRecordingsMenu.GoBack();
                                    savedRecordingsMenu.GoBack();
                                }
                                else
                                    sender.RefreshMenu(true);
                            };
                            recordItemMenu.OnMenuClose += (m) =>
                            {
                                if (!deleteConfirmPending) return;
                                deleteConfirmPending = false;
                                deleteItem.Label = "~r~Delete";
                                deleteItem.Description = "Delete the recording.";
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

            #endregion

            #region Editor

            // TODO: Scene Creator — revisit later
            /*
            var editorItem = new UIMenuItem("Scene Creator", "Create scenes with your recorded vehicle paths.");
            editorItem.SetRightLabel("→→→");
            menu.AddItem(editorItem);
            UIMenu editorMenu = new UIMenu("Scene Creator", "Scene Creator");
            editorMenu.ControlDisablingEnabled = false;
            editorItem.Activated += (sender, e) =>
            {
                sender.SwitchTo(editorMenu, inheritOldMenuParams: true);
            };

            var createSceneItem = new UIMenuItem("Create Scene", "Create a scene with your recorded vehicle paths.");
            editorMenu.AddItem(createSceneItem);
            createSceneItem.Activated += async (sender, e) =>
            {
                MenuHandler.CloseAndClearHistory();
                Freecam.SetFreeCamActive(true);
            };
            */

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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using FxEvents;
using ScaleformUI;
using ScaleformUI.Menu;

namespace RecM.Client.Menus
{
    public class RecordingManager
    {
        #region Fields

        private static UIMenu menu;

        #endregion

        #region Constructor

        public RecordingManager()
        {
            Main.Instance.RegisterKeyMapping("recm_menu", "Vehicle Recording Utility.", "F7", new Action<int, List<object>, string>(async (source, args, rawCommand) =>
            {
                bool success = await EventDispatcher.Get<bool>("RecM:openMenu:Server");
                if (success)
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

            UIMenuItem savedRecordingsMenuItem = new UIMenuItem("Saved Recordings", "This menu contains all the saved recordings.");
            savedRecordingsMenuItem.SetRightLabel("→→→");
            menu.AddItem(savedRecordingsMenuItem);
            UIMenu savedRecordingsMenu = new UIMenu("Saved Recordings", "All the saved recordings");
            savedRecordingsMenuItem.Activated += (sender, e) =>
            {
                sender.SwitchTo(savedRecordingsMenu, inheritOldMenuParams: true);
            };

            UIMenuItem vanillaRecordingsMenuItem = new UIMenuItem("Vanilla", "This menu contains all the vanilla recording data.");
            vanillaRecordingsMenuItem.SetRightLabel("→→→");
            savedRecordingsMenu.AddItem(vanillaRecordingsMenuItem);
            UIMenu vanillaRecordingsMenu = new UIMenu("Vanilla", "All the vanilla recordings.");
            vanillaRecordingsMenuItem.Activated += (sender, e) =>
            {
                sender.SwitchTo(vanillaRecordingsMenu, inheritOldMenuParams: true);
            };

            UIMenuItem customRecordingsMenuItem = new UIMenuItem("Custom", "This menu contains all the custom recording data.");
            customRecordingsMenuItem.SetRightLabel("→→→");
            savedRecordingsMenu.AddItem(customRecordingsMenuItem);
            UIMenu customRecordingsMenu = new UIMenu("Custom", "All the custom recordings.");
            customRecordingsMenuItem.Activated += (sender, e) =>
            {
                sender.SwitchTo(customRecordingsMenu, inheritOldMenuParams: true);
            };

            menu.OnMenuOpen += async (menu, data) =>
            {
                vanillaRecordingsMenu.Clear();
                customRecordingsMenu.Clear();
                savedRecordingsMenuItem.Enabled = false;
                savedRecordingsMenuItem.Description = "Loading...";
                savedRecordingsMenuItem.SetRightBadge(BadgeIcon.LOCK);

                (List<string> vanilla, List<string> custom) = await Recording.GetRecordings();

                savedRecordingsMenuItem.Enabled = true;
                savedRecordingsMenuItem.Description = "This menu contains all the saved recordings.";
                savedRecordingsMenuItem.SetRightBadge(BadgeIcon.NONE);

                Dictionary<string, List<string>> vanillaRecordings = [];
                foreach (var recording in vanilla)
                {
                    string name = recording.Substring(0, recording.Length - 3);
                    string id = int.Parse(recording.Substring(recording.Length - 3)).ToString();

                    if (!vanillaRecordings.ContainsKey(name))
                        vanillaRecordings.Add(name, [id]);
                    else
                        vanillaRecordings[name].Add(id);
                }

                foreach (var recording in vanillaRecordings)
                {
                    if (!vanillaRecordingsMenu.MenuItems.Any(x => x.Label.Equals(recording.Key)))
                    {
                        var listItem = new UIMenuListItem(recording.Key, [], 0);
                        vanillaRecordingsMenu.AddItem(listItem);
                        foreach (var id in recording.Value)
                            listItem.Items.Add(id);

                        // Reorder the items by ID from lowest to highest
                        listItem.Items = listItem.Items.OrderBy(x => x).ToList();

                        listItem.OnListSelected += (item, index) =>
                        {
                            if (Recording.LoadingRecording)
                            {
                                "There's a recording being loaded at the moment, please wait.".Log();
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

                foreach (var recording in custom)
                {
                    var name = recording.Split('_')[0];
                    var model = recording.Split('_')[1];
                    var id = int.Parse(recording.Split('_')[2]);

                    UIMenuItem recordItem = new UIMenuItem(name, $"Vehicle: {model}\nID: {id}");
                    recordItem.ItemData = recording;
                    recordItem.SetRightLabel("→→→");
                    customRecordingsMenu.AddItem(recordItem);
                    UIMenu recordItemMenu = new UIMenu(name, name);
                    recordItem.Activated += (sender, e) =>
                    {
                        sender.SwitchTo(recordItemMenu, inheritOldMenuParams: true);
                    };

                    UIMenuItem playItem = new UIMenuItem("Play", "Play the recording.");
                    recordItemMenu.AddItem(playItem);
                    playItem.Activated += async (sender, e) =>
                    {
                        Recording.PlayRecording(id, $"{name}_{model}_", model);
                    };
                }
            };
        }

        #endregion

        #endregion
    }
}

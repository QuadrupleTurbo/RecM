using System;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;
using FxEvents;
using RecM.Client.Menus;

namespace RecM.Client
{
    public class Main : BaseScript
    {
        #region Properties

        /// <summary>
        /// We can tap into ScaleformUI's game time which is called every 100ms.
        /// </summary>
        public static long GameTime
        {
            get => ScaleformUI.Main.GameTime;
        }

        #endregion

        #region Fields

        public static Main Instance;
        public ExportDictionary ExportList;
        private bool _firstTick;
        private bool _isResourceValid;
        private readonly string _resourceName = API.GetCurrentResourceName();
        public bool DebugMode;

        #endregion

        #region Constructor

        public Main()
        {
            EventDispatcher.Initalize("de5WwZCPjcmx5kH2f97a", "HqDMBdqUQvFsx8ivY0sb", "gcNmjnWvG3VQRJMYXV50", "VjqQBxkkV3r4wd105W24");
            Instance = this;
            ExportList = Exports;
            string debugMode = API.GetResourceMetadata(API.GetCurrentResourceName(), "recm_debug_mode", 0);
            DebugMode = debugMode == "yes" || debugMode == "true" || int.TryParse(debugMode, out int num) && num > 0;

            if (_resourceName == "RecM")
            {
                // Load classes
                new Recording();
                new RecordingManager();
            }
            else
                "The resource name is invalid, please name it to RecM!".Error(true);

            // Debug
            Screen.Fading.FadeIn(0);
        }

        #endregion

        #region Tools

        #region Add event handler statically

        public void AddEventHandler(string eventName, Delegate @delegate, bool oldMethod = false)
        {
            if (!oldMethod)
                EventDispatcher.Mount(eventName, @delegate);
            else
                EventHandlers.Add(eventName, @delegate);
        }

        #endregion

        #region Attach tick statically

        public void AttachTick(Func<Task> task)
        {
            Tick += task;
            $"Attached tick: {task.Method.Name}".Log();
        }

        #endregion

        #region Detach tick statically

        public void DetachTick(Func<Task> task)
        {
            Tick -= task;
            $"Detached tick: {task.Method.Name}".Log();
        }

        #endregion

        #region Register key mapping

        public void RegisterKeyMapping(string command, string description, string defaultKey, Delegate @delegate)
        {
            API.RegisterKeyMapping(command, description, "keyboard", defaultKey);
            API.RegisterCommand("recm_menu", @delegate, false);
        }

        #endregion

        #endregion
    }
} 
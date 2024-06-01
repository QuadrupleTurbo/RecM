using System;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using FxEvents;

namespace RecM.Server
{
    public class Main : BaseScript
    {
        #region Fields

        public static Main Instance;
        public PlayerList Clients;
        public ExportDictionary ExportList;
        private readonly string _resourceName = API.GetCurrentResourceName();
        public bool DebugMode;

        #endregion

        #region Constructor

        public Main()
        {
            EventDispatcher.Initalize("de5WwZCPjcmx5kH2f97a", "HqDMBdqUQvFsx8ivY0sb", "gcNmjnWvG3VQRJMYXV50", "VjqQBxkkV3r4wd105W24");
            Instance = this;
            Clients = Players;
            ExportList = Exports;
            string debugMode = API.GetResourceMetadata(API.GetCurrentResourceName(), "recm_debug_mode", 0);
            DebugMode = debugMode == "yes" || debugMode == "true" || int.TryParse(debugMode, out int num) && num > 0;

            if (_resourceName == "RecM")
            {
                // Load classes
                new Recording();
            }
            else
                "The resource name is invalid, please name it to RecM!".Error();
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

        #endregion
    }
}
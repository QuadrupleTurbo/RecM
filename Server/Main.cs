using System;
using CitizenFX.Core;
using FxEvents;

namespace RecM.Server
{
    public class Main : BaseScript
    {
        #region Fields

        public static Main Instance;
        public PlayerList Clients;
        public ExportDictionary ExportList;

        #endregion

        #region Constructor

        public Main()
        {
            EventDispatcher.Initalize("de5WwZCPjcmx5kH2f97a", "HqDMBdqUQvFsx8ivY0sb", "gcNmjnWvG3VQRJMYXV50", "VjqQBxkkV3r4wd105W24");
            Instance = this;
            Clients = Players;
            ExportList = Exports;

            // Load classes
            new Recording();
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
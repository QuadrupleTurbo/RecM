using System;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using FxEvents;

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

        #endregion

        #region Constructor

        public Main()
        {
            EventDispatcher.Initalize("de5WwZCPjcmx5kH2f97a", "HqDMBdqUQvFsx8ivY0sb", "gcNmjnWvG3VQRJMYXV50", "VjqQBxkkV3r4wd105W24");
            Instance = this;
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
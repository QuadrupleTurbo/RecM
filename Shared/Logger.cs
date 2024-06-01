using CitizenFX.Core;
using System.Collections.Generic;

#if CLIENT

using CitizenFX.Core.UI;
using CitizenFX.Core.Native;
using RecM.Client;
using System.Drawing;

#endif

#if SERVER

using System.Runtime.CompilerServices;
using RecM.Server;

#endif

namespace RecM
{
    internal static class Logger
    {
        #region Fields

#if CLIENT

        private static long _helpTextTimer = 0;

#endif

        #endregion

        #region Tools

#if CLIENT

        #region Log/success

        /// <summary>
        /// General logs.
        /// </summary>
        /// <param name="msg"></param>
        internal static void Log(this string msg, bool notify = false)
        {
            if (string.IsNullOrEmpty(msg)) return;

            if (Main.Instance.DebugMode)
            {
                // Extracted each string from each "row" which would be a string separated with the \n character
                List<string> rows = [.. msg.Split('\n')];

                // Interate through the rows and fix up the strings
                List<string> modifiedRows = [];
                foreach (var row in rows)
                {
                    List<string> currRow = [];
                    foreach (var word in row.Split())
                    {
                        var yes = word.Insert(0, "^2");
                        currRow.Add(yes);
                    }

                    modifiedRows.Add(string.Join(" ", currRow));
                }

                // Make a new line
                Debug.WriteLine();

                // Now let's print the rows
                foreach (var row in modifiedRows)
                    Debug.WriteLine($"[RecM Logs] [{new System.Diagnostics.StackFrame(1).GetMethod().Name}] " + row + "^7");

                // Make a new line
                Debug.WriteLine();
            }

            if (notify)
            {
                API.ThefeedSetScriptedMenuHeight(0.41f);
                API.SetNotificationTextEntry("CELL_EMAIL_BCON");
                foreach (string s in Screen.StringToArray(msg))
                    API.AddTextComponentSubstringPlayerName(s);
                API.SetNotificationBackgroundColor(184); // gyazo.com/68bd384455fceb0a85a8729e48216e15
                API.SetNotificationMessage("recm_textures", "recm_notification", true, 4, "RecM", "Success");
                API.DrawNotification(false, false);
            }
        }

        #endregion

        #region Alert

        /// <summary>
        /// Warning logs.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="notify"></param>
        internal static void Alert(this string msg, bool notify = false)
        {
            if (string.IsNullOrEmpty(msg)) return;

            if (Main.Instance.DebugMode)
            {
                // Extracted each string from each "row" which would be a string separated with the \n character
                List<string> rows = [.. msg.Split('\n')];

                // Interate through the rows and fix up the strings
                List<string> modifiedRows = [];
                foreach (var row in rows)
                {
                    List<string> currRow = [];
                    foreach (var word in row.Split())
                    {
                        var yes = word.Insert(0, "^5");
                        currRow.Add(yes);
                    }

                    modifiedRows.Add(string.Join(" ", currRow));
                }

                // Make a new line
                Debug.WriteLine();

                // Now let's print the rows
                foreach (var row in modifiedRows)
                    Debug.WriteLine($"[RecM Alerts] [{new System.Diagnostics.StackFrame(1).GetMethod().Name}] " + row + "^7");

                // Make a new line
                Debug.WriteLine();
            }

            if (notify)
            {
                API.ThefeedSetScriptedMenuHeight(0.41f);
                API.SetNotificationTextEntry("CELL_EMAIL_BCON");
                foreach (string s in Screen.StringToArray(msg))
                    API.AddTextComponentSubstringPlayerName(s);
                API.SetNotificationBackgroundColor(40); // gyazo.com/68bd384455fceb0a85a8729e48216e15
                API.SetNotificationMessage("recm_textures", "recm_notification", true, 4, "RecM", "Alert");
                API.DrawNotification(true, false);
            }
        }

        #endregion

        #region Warning

        /// <summary>
        /// Warning logs.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="notify"></param>
        internal static void Warning(this string msg, bool notify = false)
        {
            if (string.IsNullOrEmpty(msg)) return;

            if (Main.Instance.DebugMode)
            {
                // Extracted each string from each "row" which would be a string separated with the \n character
                List<string> rows = [.. msg.Split('\n')];

                // Interate through the rows and fix up the strings
                List<string> modifiedRows = [];
                foreach (var row in rows)
                {
                    List<string> currRow = [];
                    foreach (var word in row.Split())
                    {
                        var yes = word.Insert(0, "^3");
                        currRow.Add(yes);
                    }

                    modifiedRows.Add(string.Join(" ", currRow));
                }

                // Make a new line
                Debug.WriteLine();

                // Now let's print the rows
                foreach (var row in modifiedRows)
                    Debug.WriteLine($"[Warnings] [{new System.Diagnostics.StackFrame(1).GetMethod().Name}] " + row + "^7");

                // Make a new line
                Debug.WriteLine();
            }

            if (notify)
            {
                API.ThefeedSetScriptedMenuHeight(0.41f);
                API.SetNotificationTextEntry("CELL_EMAIL_BCON");
                foreach (string s in Screen.StringToArray(msg))
                    API.AddTextComponentSubstringPlayerName(s);
                API.SetNotificationBackgroundColor(190); // gyazo.com/68bd384455fceb0a85a8729e48216e15
                API.SetNotificationMessage("recm_textures", "recm_notification", true, 4, "RecM", "Warning");
                API.DrawNotification(true, false);
            }
        }

        #endregion

        #region Error

        /// <summary>
        /// Error logs.
        /// </summary>
        /// <param name="msg"></param>
        internal static void Error(this string msg, bool notify = false)
        {
            if (string.IsNullOrEmpty(msg)) return;

            if (Main.Instance.DebugMode)
            {
                // Extracted each string from each "row" which would be a string separated with the \n character
                List<string> rows = [.. msg.Split('\n')];

                // Interate through the rows and fix up the strings
                List<string> modifiedRows = [];
                foreach (var row in rows)
                {
                    List<string> currRow = [];
                    foreach (var word in row.Split())
                    {
                        var yes = word.Insert(0, "^1");
                        currRow.Add(yes);
                    }

                    modifiedRows.Add(string.Join(" ", currRow));
                }

                // Make a new line
                Debug.WriteLine();

                // Now let's print the rows
                foreach (var row in modifiedRows)
                    Debug.WriteLine($"[Errors] [{new System.Diagnostics.StackFrame(1).GetMethod().Name}] " + row + "^7");

                // Make a new line
                Debug.WriteLine();
            }

            if (notify)
            {
                API.ThefeedSetScriptedMenuHeight(0.41f);
                API.SetNotificationTextEntry("CELL_EMAIL_BCON");
                foreach (string s in Screen.StringToArray(msg))
                    API.AddTextComponentSubstringPlayerName(s);
                API.SetNotificationBackgroundColor(6); // gyazo.com/68bd384455fceb0a85a8729e48216e15
                API.SetNotificationMessage("recm_textures", "recm_notification", true, 4, "RecM", "Error");
                API.DrawNotification(true, false);
            }
        }

        #endregion

        #region Help

        internal static void HelpThisFrame(this string msg)
        {
            Screen.DisplayHelpTextThisFrame(msg);
            if (API.IsHelpMessageBeingDisplayed())
            {
                // Basically, we can interfere with help texts that have been given a duration, but not other every frame help messages
                if (_helpTextTimer - Main.GameTime > 0)
                {
                    API.ClearAllHelpMessages();
                    API.EndTextCommandDisplayHelp(0, false, true, 0);
                    Screen.DisplayHelpTextThisFrame(msg);
                }
            }
        }
        internal static void Help(this string msg) => Help(msg, 6000);
        internal static void Help(this string msg, int duration)
        {
            if (string.IsNullOrEmpty(msg)) return;

            string[] array = Screen.StringToArray(msg);
            if (API.IsHelpMessageBeingDisplayed())
            {
                API.ClearAllHelpMessages();
            }
            API.BeginTextCommandDisplayHelp("CELL_EMAIL_BCON");
            foreach (string s in array)
            {
                API.AddTextComponentSubstringPlayerName(s);
            }
            API.EndTextCommandDisplayHelp(0, false, true, duration);

            _helpTextTimer = Main.GameTime + duration;
        }

        #endregion

        #region Subtitle

        /// <summary>
        /// Draw a subtitle every frame.
        /// </summary>
        /// <param name="msg"></param>
        internal static void Subtitle(this string msg) => Subtitle(msg, 0, Color.FromArgb(190, 240, 255));

        /// <summary>
        /// Draw a subtitle every frame with a custom colour RGB.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="colour"></param>
        internal static void Subtitle(this string msg, Color colour) => Subtitle(msg, 0, colour);

        /// <summary>
        /// Draw a subtitle for an interval in milliseconds.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="duration"></param>
        internal static void Subtitle(this string msg, int duration) => Subtitle(msg, duration, Color.FromArgb(190, 240, 255));

        /// <summary>
        /// Draw a subtitle for an interval in milliseconds with a custom colour RGB.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="duration"></param>
        /// <param name="colour"></param>
        internal static void Subtitle(this string msg, int duration, Color colour) => Screen.ShowSubtitle($"<FONT COLOR=\'#{colour.R:X2}{colour.G:X2}{colour.B:X2}\'>{msg}", duration);

        #endregion

#endif

#if SERVER

        #region Log/success

        /// <summary>
        /// General logs.
        /// </summary>
        /// <param name="msg"></param>
        internal static void Log(this string msg, [CallerMemberName] string callerName = "")
        {
            if (string.IsNullOrEmpty(msg)) return;

            // Extracted each string from each "row" which would be a string separated with the \n character
            List<string> rows = [.. msg.Split('\n')];

            // Interate through the rows and fix up the strings
            List<string> modifiedRows = [];
            foreach (var row in rows)
            {
                List<string> currRow = [];
                foreach (var word in row.Split())
                {
                    var yes = word.Insert(0, "^2");
                    currRow.Add(yes);
                }

                modifiedRows.Add(string.Join(" ", currRow));
            }

            // Make a new line
            Debug.WriteLine();

            // Now let's print the rows
            foreach (var row in modifiedRows)
                Debug.WriteLine($"[RecM Logs] [{callerName}] " + row + "^7");

            // Make a new line
            Debug.WriteLine();
        }

        #endregion

        #region Warning

        /// <summary>
        /// Warning logs.
        /// </summary>
        /// <param name="msg"></param>
        internal static void Warning(this string msg, [CallerMemberName] string callerName = "")
        {
            if (string.IsNullOrEmpty(msg)) return;

            // Extracted each string from each "row" which would be a string separated with the \n character
            List<string> rows = [.. msg.Split('\n')];

            // Interate through the rows and fix up the strings
            List<string> modifiedRows = [];
            foreach (var row in rows)
            {
                List<string> currRow = [];
                foreach (var word in row.Split())
                {
                    var yes = word.Insert(0, "^3");
                    currRow.Add(yes);
                }

                modifiedRows.Add(string.Join(" ", currRow));
            }

            // Make a new line
            Debug.WriteLine();

            // Now let's print the rows
            foreach (var row in modifiedRows)
                Debug.WriteLine($"[RecM Warnings] [{callerName}] " + row + "^7");

            // Make a new line
            Debug.WriteLine();
        }

        #endregion

        #region Error

        /// <summary>
        /// Error logs.
        /// </summary>
        /// <param name="msg"></param>
        internal static void Error(this string msg, [CallerMemberName] string callerName = "")
        {
            if (string.IsNullOrEmpty(msg)) return;

            // Extracted each string from each "row" which would be a string separated with the \n character
            List<string> rows = [.. msg.Split('\n')];

            // Interate through the rows and fix up the strings
            List<string> modifiedRows = new List<string>();
            foreach (var row in rows)
            {
                List<string> currRow = new List<string>();
                foreach (var word in row.Split())
                {
                    var yes = word.Insert(0, "^1");
                    currRow.Add(yes);
                }

                modifiedRows.Add(string.Join(" ", currRow));
            }

            // Make a new line
            Debug.WriteLine();

            // Now let's print the rows
            foreach (var row in modifiedRows)
                Debug.WriteLine($"[RecM Errors] [{callerName}] " + row + "^7");

            // Make a new line
            Debug.WriteLine();
        }

        #endregion

#endif

        #endregion
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if SERVER

using System.IO;
using System.Xml;
using RecM.Server;
using CodeWalker.GameFiles;
using CitizenFX.Core.Native;

#endif

namespace RecM
{
    public class Recording
    {
        #region Constructor

#if SERVER

        public Recording()
        {
            Main.Instance.AddEventHandler("RecM:saveRecording:Server", new Action<string, string, string>(SaveRecording), true);
        }

#endif

        #endregion

        #region Events

#if SERVER

        #region Save recording

        private void SaveRecording(string name, string model, string data)
        {
            // The server needs to have the RecM_records resource started
            if (API.GetResourceState("RecM_records") != "started")
            {
                "You need to have the RecM_records resource present in order to save recordings.".Error();
                return;
            }

            // Parse the data
            var recordings = Json.Parse<List<Record>>(data);

            // This will be used to load/save meta files
            XmlDocument doc = new XmlDocument();

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
                // Now if we're here, the file existed, now we need to find the hgihest number and add 1 to it
                var maxValue = Directory.EnumerateFiles(recordingsPath).Where(x => x.Contains(name) && x.Contains(model)).Cast<string>().Max(x => int.Parse(Path.GetFileNameWithoutExtension(x).Split('_')[2]));
                File.WriteAllBytes(Path.Combine(recordingsPath, $"{name}_{model}_{(maxValue + 1).ToString().PadLeft(3, '0')}.yvr"), yvrData);
            }

            // Finally, refresh and restart the recordings resource (hopefully no problems client side)
            API.ExecuteCommand("refresh");
            API.ExecuteCommand("ensure RecM_records");
        }

        #endregion


        #region Clean recordings

        private static void CleanRecordings()
        {
            // If no records resource exists, don't continue and notify the server owner
            if (!Directory.Exists(API.GetResourcePath("RecM_records")))
            {
                "You need to have the RecM_records resource present in order to save recordings.".Error();
                return;
            }

            // Grab the path of the recordings resource
            var recordingsPath = Path.Combine(API.GetResourcePath("RecM_records"), "stream");

            // Find all files that are older than the current
            List<string> filesToKeep = [];
            List<string> filesToDestroy = [];
            foreach (var file in Directory.EnumerateFiles(recordingsPath))
            {
                var name = Path.GetFileNameWithoutExtension(file).Split('_')[0];
                var veh = Path.GetFileNameWithoutExtension(file).Split('_')[1];
                var num = Path.GetFileNameWithoutExtension(file).Split('_')[2];
                var maxValue = Directory.EnumerateFiles(recordingsPath).Where(x => Path.GetFileNameWithoutExtension(x).Split('_')[0].Equals(name) && Path.GetFileNameWithoutExtension(x).Split('_')[1].Equals(veh)).Cast<string>().Max(x => int.Parse(Path.GetFileNameWithoutExtension(x).Split('_')[2]));
                if (num != maxValue.ToString().PadLeft(3, '0'))
                    filesToDestroy.Add(file);
                else
                    filesToKeep.Add(file);
            }

            // Delete all the files we collected
            foreach (var file in filesToDestroy)
                File.Delete(file);

            // Rename the kept files to the lowest recording id value
            foreach (var file in filesToKeep)
            {
                var name = Path.GetFileNameWithoutExtension(file).Split('_')[0];
                var veh = Path.GetFileNameWithoutExtension(file).Split('_')[1];
                File.Move(file, Path.Combine(Path.GetDirectoryName(file), $"{name}_{veh}_001.yvr"));
            }

            // Finally, refresh and ensure the recordings resource
            API.ExecuteCommand("refresh");
            API.ExecuteCommand("ensure RecM_records");
        }

        #endregion

#endif

        #endregion
    }
}

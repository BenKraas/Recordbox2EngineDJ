// See https://aka.ms/new-console-template for more information
using BlobConvert;
using System.Reflection;
using System.Text;
using System.Xml;
using System.IO;
using System;
using System.Diagnostics;

// locale for the console to en-US
using System.Globalization;
using System.Threading;

internal class Program
{
    static void Main(string[] args)
    {
        // Set the locale to en-US
        CultureInfo.CurrentCulture = new CultureInfo("en-US");
        CultureInfo.CurrentUICulture = new CultureInfo("en-US");

        Console.WriteLine("Locale: " + CultureInfo.CurrentCulture.Name);

        try
        {
            // Check if the user provided a path to the XML file and provide the filepath variable
            if (args.Length == 0)
            {
                //Console.WriteLine("Please provide a path to the XML file.");
                args = new string[] { "C:\\Users\\Kasto\\OneDrive\\DJ-Main\\Archive of great music\\Recordbox\\2024-03-09 - Copy.xml" }; // Development purposes 
            }
            string filePath = args[0];

            // Check if the file exists
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"File '{filePath}' not found.");
                return;
            }

            // Preperation for the XML Document
            XmlDocument RCBxml = new XmlDocument();
            RCBxml.Load(args[0]);


            //string xml = File.ReadAllText(args[0]);
            //string _byteOrderMarkUtf8 = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
            //if (xml.StartsWith(_byteOrderMarkUtf8))
            //{
            //    xml = xml.Remove(0, _byteOrderMarkUtf8.Length);
            //}
            //Console.WriteLine(args[0]);

            // Select all tracks
            XmlNodeList Tracks = RCBxml.SelectNodes("DJ_PLAYLISTS/COLLECTION/TRACK");
            Console.WriteLine("Collected Tracks: " + Tracks.Count);


            // Iterate through all tracks
            foreach (XmlNode Track in Tracks)
            {
                XmlAttribute xName;     // Name of the track
                string pLocation;       // Path to the file
                double offsetValue_MS;  // Offset value of the track

                double? overrideOffset_MS = null;
                string TrackUUID = Track.Attributes["TrackID"].Value;
                string Artist = Track.Attributes["Artist"].Value;
                string Title = Track.Attributes["Name"].Value;

                // Remove any file://localhost/ from the path
                CleanXmlContent(Track, out xName, out pLocation, out offsetValue_MS);

                // If the file does not exist, skip the track
                if (!File.Exists(pLocation))
                {
                    Console.WriteLine($"File '{pLocation}' not found.");
                    continue;
                }

                // Check if the track is in SkipData
                if (OverrideDatasetHandle.Instance.TryGetDataEntry(TrackUUID, out var entry))
                {
                    if (entry.Skip)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Skipping {xName.Value}");
                        Console.ResetColor();
                        continue;
                    }
                    overrideOffset_MS = entry.OverrideOffsetValue_MS;
                }
                else
                {
                    // Add the track to the skip data
                    OverrideDatasetHandle.Instance.AddDataEntry(TrackUUID, Artist + " - " + Title);
                }

                // If the name contains the string "Acap" or "acap" then skip the track
                if (xName.Value.Contains("Acap") || xName.Value.Contains("acap"))
                {
                    // print skipping message to green text
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Skipping Acapella: {xName.Value}");
                    Console.ResetColor();

                    // Add the track to the skip data
                    OverrideDatasetHandle.Instance.AddSkippable(TrackUUID, Artist + " - " + Title);
                    continue;
                }

                // If the track contains override offset, apply it, otherwise calculate the offset
                if (overrideOffset_MS.HasValue)
                {
                    offsetValue_MS = overrideOffset_MS.Value;
                }
                else
                {
                    offsetValue_MS = SilenceDetector.DetectSilence(pLocation);
                }


                var TempoNodes = Track.SelectNodes("TEMPO");

                if (TempoNodes != null && TempoNodes.Count > 0)
                {
                    string tempoVal = TempoNodes.Item(0).Attributes["Inizio"].Value;
                    double currentOffset_S = double.Parse(tempoVal);

                    // Calculate the difference between the current offset and the new offset
                    double diff_S = currentOffset_S - (offsetValue_MS / 1000);

                    // If the difference is too large, skip the track UNLESS the offset is set by the user
                    if (Math.Abs(diff_S) > 0.08 && !overrideOffset_MS.HasValue)
                    {
                        // print error message to yellow text
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"Offset for {xName.Value} too large");
                        Console.ResetColor();

                        // Add the track to the skip data
                        OverrideDatasetHandle.Instance.AddSkippable(TrackUUID, Artist + " - " + Title);
                        continue;
                    }
                    else if (overrideOffset_MS.HasValue)
                    {
                        // print error message to Blue text
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.Write($"Offset was user-defined: ");
                        Console.ResetColor();
                    }
                    else
                    {
                        // print auto-detection message to orange text
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.Write($"Offset auto-detected: ");
                        Console.ResetColor();
                        // note to self: resist the temptation of a logger method
                    }

                    // Print the offset value and let the user know the direction of the shift
                    var shifttype = "ERROR";
                    if (diff_S > 0)
                        shifttype = "left ";
                    else
                        shifttype = "right ";

                    //disGustInG cOnsOle wRiTe, dO nOt hIrE mE
                    Console.Write($"Editing {xName.Value}, old offset: {currentOffset_S}, applying shift to ");
                    Console.ForegroundColor =
                        overrideOffset_MS.HasValue 
                            ? diff_S > 0 
                                ? ConsoleColor.Cyan 
                                : ConsoleColor.Magenta 
                            : ConsoleColor.Gray;
                    Console.Write($"{shifttype}");
                    Console.ResetColor();
                    Console.WriteLine($"by {Math.Abs(diff_S).ToString("0.000")} ms.");



                    // Apply the offset to the TEMPO nodes (Inizio)
                    foreach (XmlNode TempoNode in TempoNodes)
                    {
                        XmlAttribute xInizio = TempoNode.Attributes["Inizio"];
                        if (xInizio != null)
                        {
                            double editValue_S = double.Parse(xInizio.Value);
                            editValue_S -= diff_S;
                            xInizio.Value = editValue_S.ToString("0.000");
                        }
                    }

                    // Apply the offset to the POSITION_MARK nodes (Cues)
                    var posMarks = Track.SelectNodes("POSITION_MARK");
                    if (posMarks != null)
                    {
                        foreach (XmlNode posMarkNode in posMarks)
                        {
                            XmlAttribute xStart = posMarkNode.Attributes["Start"];
                            if (xStart != null)
                            {
                                double editValue_S = double.Parse(xStart.Value);
                                editValue_S -= diff_S;
                                xStart.Value = editValue_S.ToString("0.000");
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"No tempo node found for {xName.Value}");
                    continue;
                }
            }
            // Save the changes to the OverrideData json file
            OverrideDatasetHandle.Instance.SaveToFile();

            // Set a new file path
            string newFilePath = filePath.Replace(".xml", "_new.xml");

            // Save the document
            RCBxml.Save(newFilePath);

            // Open path to directory
            Process.Start("explorer.exe", newFilePath);


        }
        catch (Exception e)
        {

            Console.WriteLine(e.Message);
            //Console.ReadLine();
        }




        //Console.WriteLine("Soll: 108");

        //SilenceDetector.DetectSilence("C:\\Users\\Admin\\source\\repos\\BlobConvert\\BlobConvert\\Sean_Paul_-_Get_Busy_-_Deux_Twins_Remix_Intro_-_PL_CT_2024.mp3");

        //Console.WriteLine();
        //Console.WriteLine("Soll: 52");
        //SilenceDetector.DetectSilence("C:\\Users\\Admin\\source\\repos\\BlobConvert\\BlobConvert\\14265045_Vex_Original_Mix_-_P_BP_2024.mp3");

        //Console.WriteLine();
        //Console.WriteLine("Soll: 24");
        //SilenceDetector.DetectSilence("C:\\Users\\Admin\\source\\repos\\BlobConvert\\BlobConvert\\Cheat_Codes__Danny_Quest_-_NSFW_Intro_-_Dirty_-_PL_CT_2024.mp3");
    }

    private static void CleanXmlContent(XmlNode Track, out XmlAttribute xName, out string pLocation, out double offsetValue)
    {
        XmlAttribute xLocation = Track.Attributes["Location"];
        xName = Track.Attributes["Name"];
        pLocation = xLocation.Value;
        offsetValue = 0;

        // convert URL to local path
        if (pLocation.StartsWith("file://localhost/"))
        {
            pLocation = pLocation.Replace("file://localhost/", "");
            pLocation = pLocation.Replace("/", "\\");

            // Solve %20 etc in the path
            pLocation = Uri.UnescapeDataString(pLocation);
        }
    }
}

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UtilLibs;

namespace BlobConvert
{
    internal class OverrideDatasetHandle
    {
        [JsonIgnore]
        static string rffPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "OverrideDataset.json");

        // structure for the skip data
        // [
        //   TrackUUID:
        //      {
        //          "Skip": bool,
        //          "Userlocked": bool,
        //          "OverrideOffsetValue_MS": float|null
        //      },
        // ]

        // Dictionary to store the skip data
        [JsonProperty]
        private Dictionary<string, SkipDataEntry> SkipDataEntries { get; set; } = new();


        [JsonIgnore]
        private static OverrideDatasetHandle _instance;
        [JsonIgnore]
        public static OverrideDatasetHandle Instance
        {
            get
            {
                if (_instance == null)
                {
                    // get current executable path and read from subfolder cfg

                    if (IO_Utils.ReadFromFile<OverrideDatasetHandle>(rffPath, out var config))
                    {
                        _instance = config;
                    }
                    else
                    {
                        _instance = new OverrideDatasetHandle();
                    }
                }
                return _instance;
            }
        }

        public void SaveToFile()
        {
            IO_Utils.WriteToFile(this, rffPath);
        }

        /// <summary>
        /// Add a track to the skip data. 
        /// If the track is already in the skip data, it will be overwritten UNLESS the userlocked flag is set to true
        /// </summary>
        /// <param name="trackUUID"></param>
        public void AddSkippable(string trackUUID, string artistAndTitle, bool saveToFile = false)
        {
            if (SkipDataEntries.ContainsKey(trackUUID) && SkipDataEntries[trackUUID].Userlocked)
            {
                return;
            }
            SkipDataEntries[trackUUID] = new SkipDataEntry { ArtistAndTitle = artistAndTitle, Skip = true, Userlocked = false, OverrideOffsetValue_MS = null };
            if (saveToFile) SaveToFile();
        }

        public void SetOverrideOffset(string TrackUUID, float offset, bool saveToFile = false)
        {
            if (SkipDataEntries.ContainsKey(TrackUUID))
            {
                if (SkipDataEntries[TrackUUID].Userlocked)
                {
                    return;
                }
                SkipDataEntries[TrackUUID].OverrideOffsetValue_MS = offset;
                if (saveToFile) SaveToFile();
            }
        }

        public bool TryGetDataEntry(string TrackUUID, out SkipDataEntry entry)
        {
            return SkipDataEntries.TryGetValue(TrackUUID, out entry);
        }

        internal void AddDataEntry(string trackUUID, string artistAndTitle, bool skip = false, bool userlocked = false, float? overrideOffsetValue_MS = null)
        {
            SkipDataEntries[trackUUID] = new SkipDataEntry { ArtistAndTitle = artistAndTitle, Skip = skip, Userlocked = userlocked, OverrideOffsetValue_MS = overrideOffsetValue_MS };
        }

        public class SkipDataEntry
        {
            // public string TrackUUID { get; set; }
            public string ArtistAndTitle;
            public bool Skip = false;
            public bool Userlocked = false;
            public float? OverrideOffsetValue_MS = null;

        }
    }
}



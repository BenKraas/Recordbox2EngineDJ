using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using static BlobConvert.AudioFileReaderExt;

namespace BlobConvert
{
    internal class SilenceDetector
    {
        public static double DetectSilence(string filePath)
        {
            int offsetPosition = 0; TimeSpan duration;
            // Console.WriteLine( Path.GetFileNameWithoutExtension(filePath));
            using (var reader = new AudioFileReader(filePath))
            {
                duration = reader.GetSilenceDuration(AudioFileReaderExt.SilenceLocation.Start, out offsetPosition);
                //Console.WriteLine("Ist: "+duration.TotalMilliseconds);


            }
            
            //var name = Path.GetFileNameWithoutExtension(filePath)+"_NEW.mp3";
            //var folder = Directory.GetParent(filePath).FullName;

            //using (Mp3FileReader reader = new Mp3FileReader(filePath))
            //{
            //    int count = 1;
            //    Mp3Frame mp3Frame = reader.ReadNextFrame();
            //    System.IO.FileStream _fs = new System.IO.FileStream(name, System.IO.FileMode.Create, System.IO.FileAccess.Write);
            //    double totalSamples = 0;
            //    while (mp3Frame != null)
            //    {
            //        totalSamples += ((double)mp3Frame.SampleCount) / ((double)mp3Frame.SampleRate);

            //        if (totalSamples >= duration.TotalMilliseconds / 1000.0)
            //            _fs.Write(mp3Frame.RawData, 0, mp3Frame.RawData.Length);
            //        count = count + 1;
            //        mp3Frame = reader.ReadNextFrame();
            //        if (mp3Frame == null)
            //            Console.WriteLine("totalSamples: " + totalSamples);
            //    }

            //    _fs.Close();
            //}
            return duration.TotalMilliseconds;
        }
    }
}

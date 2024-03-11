using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlobConvert
{
    static class AudioFileReaderExt
    {
        public enum SilenceLocation { Start, End }
        static float max = 0;
        private static bool IsSilence(float amplitude, sbyte threshold, out double dB)
        {
            //Console.WriteLine(Math.Abs(amplitude ));
            dB = 20 * Math.Log10(Math.Abs(amplitude));
            return dB < threshold;
        }
        public static TimeSpan GetSilenceDurationOG(this AudioFileReader reader,
                                          SilenceLocation location, out int position,
                                          sbyte silenceThreshold = -86)
        {
            int counter = 0;
            bool volumeFound = false;
            bool eof = false;
            long oldPosition = reader.Position;

            var buffer = new float[reader.WaveFormat.SampleRate * 4];
            while (!volumeFound && !eof)
            {
                int samplesRead = reader.Read(buffer, 0, buffer.Length);
                if (samplesRead == 0)
                    eof = true;

                for (int n = 0; n < samplesRead; n++)
                {
                    if (IsSilence(buffer[n], silenceThreshold, out _))
                    {
                        counter++;
                    }
                    else
                    {
                        if (location == SilenceLocation.Start)
                        {
                            volumeFound = true;
                            break;
                        }
                        else if (location == SilenceLocation.End)
                        {
                            counter = 0;
                        }
                    }
                }
            }

            // reset position
            reader.Position = oldPosition;

            position = counter;

            double silenceSamples = (double)counter / reader.WaveFormat.Channels;
            double silenceDuration = (silenceSamples / reader.WaveFormat.SampleRate) * 1000;
            return TimeSpan.FromMilliseconds(silenceDuration);
        }
        public static TimeSpan GetSilenceDuration(this AudioFileReader reader, 
                                                  SilenceLocation location, out int position,
                                                  sbyte silenceThreshold = -86)
        {
            position = 0;
            int counter = 0, silenceCounter = 0;
            bool volumeFound = false;
            bool eof = false;
            long oldPosition = reader.Position;

            var buffer = new float[reader.WaveFormat.SampleRate * 2];
            
            while (!volumeFound && !eof)
            {
                int samplesRead = reader.Read(buffer, 0, buffer.Length);
                if (samplesRead == 0)
                    eof = true;
                double db, dbOld;
                //int nullVal = (int)(0.052f * reader.WaveFormat.SampleRate * reader.WaveFormat.Channels);
                //Console.WriteLine("Nullval: " + nullVal);
                //counter += nullVal;

                for (int n = 0; n < samplesRead; n++)
                {

                    if (IsSilence(buffer[n], silenceThreshold, out db))
                    {
                        counter++; silenceCounter = 0;
                        continue;
                    }
                    else
                    {
                        if (silenceCounter < 1250)
                        {
                            counter++; silenceCounter++;
                            continue;
                        }

                        if (location == SilenceLocation.Start)
                        {
                            volumeFound = true;
                            break;
                        }
                        else if (location == SilenceLocation.End)
                        {
                            counter = 0;
                        }
                    }

                }
            }

            // reset position
            reader.Position = oldPosition;

            double silenceSamples = (double)counter / reader.WaveFormat.Channels;
            double silenceDuration = (silenceSamples / reader.WaveFormat.SampleRate) * 1000;
            // Console.WriteLine("SampleRate: " + reader.WaveFormat.SampleRate);
            // Console.WriteLine("Channels: " + reader.WaveFormat.Channels);
            position = counter;
            return TimeSpan.FromMilliseconds(silenceDuration);
        }
    }
}

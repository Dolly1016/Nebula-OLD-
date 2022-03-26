using System.IO;
using System.Reflection;

namespace Nebula.Module
{
    class AudioLoader
    {
        // convert two bytes to one float in the range -1 to 1
        static float BytesToFloat(byte firstByte, byte secondByte)
        {
            // convert two bytes to one short (little endian)
            short s = (short)((secondByte << 8) | firstByte);
            // convert to range from -1 to (just below) 1
            return s / 32768.0F;
        }

        static int BytesToInt(byte[] bytes, int offset = 0)
        {
            int value = 0;
            for (int i = 0; i < 4; i++)
            {
                value |= ((int)bytes[offset + i]) << (i * 8);
            }
            return value;
        }

        private static byte[] GetBytes(string filename)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Stream stream = assembly.GetManifestResourceStream(filename);
            var bytes = new byte[stream.Length];
            var read = stream.Read(bytes, 0, (int)stream.Length);
            return bytes;
            // return File.ReadAllBytes(filename);
        }
        // properties
        public float[] RawData { get; internal set; }
        public int ChannelCount { get; internal set; }
        public int SampleCount { get; internal set; }
        public int Frequency { get; internal set; }

        // Returns left and right double arrays. 'right' will be null if sound is mono.
        public AudioLoader(string filename) :
            this(GetBytes(filename))
        { }

        //16bitのみ対応(余計なチャンクを入れないこと)
        public AudioLoader(byte[] wav)
        {
            // Determine if mono or stereo
            ChannelCount = wav[22];     // Forget byte 23 as 99.999% of WAVs are 1 or 2 channels

            // Get the frequency
            Frequency = BytesToInt(wav, 24);

            // Get past all the other sub chunks to get to the data subchunk:
            int pos = 12;   // First Subchunk ID from 12 to 16

            // Keep iterating until we find the data chunk (i.e. 64 61 74 61 ...... (i.e. 100 97 116 97 in decimal))
            while (!(wav[pos] == 100 && wav[pos + 1] == 97 && wav[pos + 2] == 116 && wav[pos + 3] == 97))
            {
                pos += 4;
                int chunkSize = wav[pos] + wav[pos + 1] * 256 + wav[pos + 2] * 65536 + wav[pos + 3] * 16777216;
                pos += 4 + chunkSize;
            }
            pos += 8;

            // Pos is now positioned to start of actual sound data.
            SampleCount = (wav.Length - pos) / 2;     // 2 bytes per sample (16 bit sound mono)

            // Allocate memory (right will be null if only mono sound)
            RawData = new float[SampleCount];
            
            // Write to double array/s:
            int i = 0;
            while (pos < wav.Length && i < SampleCount)
            {
                RawData[i] = BytesToFloat(wav[pos], wav[pos + 1]);
                pos += 2;
                
                i++;
            }
        }
    }
}

using System;
using System.Diagnostics;
using System.Text;

namespace FrameWorkExploration {
    internal class TextEncoding {
        internal void Run() {
            // Example string
            //string originalString = "冷刺咲";
            string originalString = "abc";
            //string originalString = "قثسلثضشللم";

            //string anotherText = "冷刺咲1";
            // Convert to byte array using ISO-2022-JP encoding

            var encoder = Encoding.GetEncoding(
                "csISO2022JP",
                new EncoderReplacementFallback(),
                new DecoderReplacementFallback());

            var stopWatch = Stopwatch.StartNew();
            byte[] iso2022jpBytes = encoder.GetBytes(originalString);

            // Convert back to string
            string iso2022jpString = encoder.GetString(iso2022jpBytes);

            stopWatch.Stop();
            Console.WriteLine($"Elapsed time: {stopWatch.ElapsedTicks} ticks");

            var isSame = originalString.Equals(iso2022jpString);

            // Display results
            Console.WriteLine($"Original String: {originalString}");
            Console.WriteLine($"Converted Back: {iso2022jpString}");

            

        }
    }
}

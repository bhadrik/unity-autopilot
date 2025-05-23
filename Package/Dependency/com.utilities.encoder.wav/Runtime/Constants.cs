// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Utilities.Encoding.Wav
{
    internal static class Constants
    {
        public const int WavHeaderSize = 44;
        private const string RIFF = "RIFF";
        public static readonly byte[] RIFF_BYTES = System.Text.Encoding.ASCII.GetBytes(RIFF);
        private const string WAVE = "WAVE";
        public static readonly byte[] WAVE_BYTES = System.Text.Encoding.ASCII.GetBytes(WAVE);
        private const string FMT = "fmt ";
        public static readonly byte[] FMT_BYTES = System.Text.Encoding.ASCII.GetBytes(FMT);
        private const string DATA = "data";
        public static readonly byte[] DATA_BYTES = System.Text.Encoding.ASCII.GetBytes(DATA);
    }
}

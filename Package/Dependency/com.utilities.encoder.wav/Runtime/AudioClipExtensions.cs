// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Utilities.Async;
using Utilities.Audio;

namespace Utilities.Encoding.Wav
{
    /// <summary>
    /// https://docs.fileformat.com/audio/wav/
    /// </summary>
    public static class AudioClipExtensions
    {
        [Obsolete("Use new overload with bitDepth")]
        public static byte[] EncodeToWav(this AudioClip audioClip, bool trim)
            => EncodeToWav(audioClip, PCMFormatSize.SixteenBit, trim);

        /// <summary>
        /// Converts an <see cref="AudioClip"/> to WAV encoded memory stream.
        /// </summary>
        /// <param name="audioClip"><see cref="AudioClip"/> to convert.</param>
        /// <param name="bitDepth">Optional, bit depth to encode. Defaults to <see cref="PCMFormatSize.SixteenBit"/>.</param>
        /// <param name="trim">Optional, trim the silence at beginning and end.</param>
        /// <param name="outputSampleRate">Optional, the expected sample rate. Defaults to 44100.</param>
        /// <returns><see cref="AudioClip"/> encoded to WAV as byte array.</returns>
        public static byte[] EncodeToWav(this AudioClip audioClip, PCMFormatSize bitDepth = PCMFormatSize.SixteenBit, bool trim = false, int outputSampleRate = 44100)
        {
            if (audioClip == null) { throw new ArgumentNullException(nameof(audioClip)); }
            var bitsPerSample = 8 * (int)bitDepth;
            var sampleRate = audioClip.frequency;
            var channels = audioClip.channels;
            var pcmData = audioClip.EncodeToPCM(bitDepth, trim, outputSampleRate);
            return WavEncoder.EncodeWav(pcmData, channels, sampleRate, bitsPerSample);
        }

        [Obsolete("Use new overload with bitDepth")]
        public static async Task<byte[]> EncodeToWavAsync(this AudioClip audioClip, bool trim)
            => await EncodeToWavAsync(audioClip, PCMFormatSize.SixteenBit, trim);

        /// <summary>
        /// Converts an <see cref="AudioClip"/> to WAV encoded memory stream.
        /// </summary>
        /// <param name="audioClip"><see cref="AudioClip"/> to convert.</param>
        /// <param name="bitDepth">Optional, bit depth to encode. Defaults to <see cref="PCMFormatSize.SixteenBit"/>.</param>
        /// <param name="trim">Optional, trim the silence at beginning and end.</param>
        /// <param name="outputSampleRate">Optional, the expected sample rate. Defaults to 44100.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns><see cref="MemoryStream"/>.</returns>
        public static async Task<byte[]> EncodeToWavAsync(this AudioClip audioClip, PCMFormatSize bitDepth = PCMFormatSize.SixteenBit, bool trim = false, int outputSampleRate = 44100, CancellationToken cancellationToken = default)
        {
            if (audioClip == null) { throw new ArgumentNullException(nameof(audioClip)); }
            await Awaiters.UnityMainThread; // ensure we're on main thread, so we can access unity apis
            cancellationToken.ThrowIfCancellationRequested();
            var bitsPerSample = 8 * (int)bitDepth;
            var sampleRate = audioClip.frequency;
            var channels = audioClip.channels;
            var pcmData = audioClip.EncodeToPCM(bitDepth, trim, outputSampleRate);
            await Awaiters.BackgroundThread; // switch to background thread to prevent blocking main thread
            cancellationToken.ThrowIfCancellationRequested();
            var encodedBytes = WavEncoder.EncodeWav(pcmData, channels, sampleRate, bitsPerSample);
            await Awaiters.UnityMainThread; // return to main thread before returning result
            cancellationToken.ThrowIfCancellationRequested();
            return encodedBytes;
        }
    }
}

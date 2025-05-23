// Licensed under the MIT License. See LICENSE in the project root for license information.

using NUnit.Framework;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Utilities.Audio;

namespace Utilities.Encoding.Wav.Tests
{
    public class TestFixture_01_WAV_Encoding
    {
        private const int Channels = 1;
        private const int Frequency = 44100;

        [Test]
        public void Test_01_EncodeToWav()
        {
            // Load 16-bit PCM sample
            var raw8BitPcmPath = AssetDatabase.GUIDToAssetPath("4b0ff615f23916d4cb3fae60a50ef93c");
            Assert.IsTrue(File.Exists(raw8BitPcmPath), "16-bit PCM sample file not found");

            // Read PCM bytes
            var pcm8BitBytes = File.ReadAllBytes(raw8BitPcmPath);
            Assert.IsNotNull(pcm8BitBytes, "Failed to read 16-bit PCM bytes");
            Assert.IsNotEmpty(pcm8BitBytes, "16-bit PCM bytes array is empty");

            // Decode PCM bytes
            var samples = PCMEncoder.Decode(pcm8BitBytes);
            Assert.IsNotNull(samples, "Failed to decode PCM bytes");
            Assert.IsNotEmpty(samples, "Decoded samples array is empty");

            // Create AudioClip
            var audioClip = AudioClip.Create("16bit-sine", samples.Length, Channels, Frequency, false);
            Assert.IsNotNull(audioClip, "Failed to create AudioClip");
            audioClip.SetData(samples, 0);

            // Encode to WAV
            var encodedBytes = audioClip.EncodeToWav();

            // Validate the result
            Assert.IsNotNull(encodedBytes, "Failed to encode AudioClip to WAV");
            Assert.IsNotEmpty(encodedBytes, "Encoded WAV bytes array is empty");

            // Check WAV header
            Assert.AreEqual(samples.Length, (encodedBytes.Length - Constants.WavHeaderSize) / 2, "Unexpected WAV header size");
            Assert.AreEqual('R', encodedBytes[0], "Incorrect RIFF header");
            Assert.AreEqual('I', encodedBytes[1], "Incorrect RIFF header");
            Assert.AreEqual('F', encodedBytes[2], "Incorrect RIFF header");
            Assert.AreEqual('F', encodedBytes[3], "Incorrect RIFF header");

            // Check WAV format
            Assert.AreEqual('W', encodedBytes[8], "Incorrect WAVE header");
            Assert.AreEqual('A', encodedBytes[9], "Incorrect WAVE header");
            Assert.AreEqual('V', encodedBytes[10], "Incorrect WAVE header");
            Assert.AreEqual('E', encodedBytes[11], "Incorrect WAVE header");

            // Check format chunk
            Assert.AreEqual('f', encodedBytes[12], "Incorrect fmt header");
            Assert.AreEqual('m', encodedBytes[13], "Incorrect fmt header");
            Assert.AreEqual('t', encodedBytes[14], "Incorrect fmt header");
            Assert.AreEqual(' ', encodedBytes[15], "Incorrect fmt header");

            // Check format length
            Assert.AreEqual(16, BitConverter.ToInt32(encodedBytes, 16), "Incorrect format chunk length");

            // Check audio format (PCM)
            Assert.AreEqual(1, BitConverter.ToUInt16(encodedBytes, 20), "Incorrect audio format (PCM)");

            // Check number of channels
            Assert.AreEqual(Channels, BitConverter.ToUInt16(encodedBytes, 22), "Incorrect number of channels");

            // Check sample rate
            Assert.AreEqual(Frequency, BitConverter.ToUInt32(encodedBytes, 24), "Incorrect sample rate");

            // Check byte rate
            Assert.AreEqual(Frequency * 2, BitConverter.ToUInt32(encodedBytes, 28), "Incorrect byte rate");

            // Check block align
            Assert.AreEqual(2, BitConverter.ToUInt16(encodedBytes, 32), "Incorrect block align");

            // Check bits per sample
            Assert.AreEqual(16, BitConverter.ToUInt16(encodedBytes, 34), "Incorrect bits per sample");

            // Check data header
            Assert.AreEqual('d', encodedBytes[36], "Incorrect data header");
            Assert.AreEqual('a', encodedBytes[37], "Incorrect data header");
            Assert.AreEqual('t', encodedBytes[38], "Incorrect data header");
            Assert.AreEqual('a', encodedBytes[39], "Incorrect data header");

            // Check data length
            Assert.AreEqual(samples.Length, BitConverter.ToUInt32(encodedBytes, 40) / 2, "Incorrect data length");
        }
    }
}

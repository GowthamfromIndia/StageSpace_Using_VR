using System;
using System.IO;
using UnityEngine;

public static class WavUtility
{
    /// <summary>
    /// Converts an AudioClip to a WAV byte array.
    /// </summary>
    public static byte[] FromAudioClip(AudioClip clip)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                WriteWavHeader(writer, clip);
                WriteWavData(writer, clip);
            }
            return stream.ToArray();
        }
    }

    /// <summary>
    /// Converts a WAV byte array to an AudioClip.
    /// </summary>
    public static AudioClip ToAudioClip(byte[] wavData)
    {
        using (MemoryStream stream = new MemoryStream(wavData))
        using (BinaryReader reader = new BinaryReader(stream))
        {
            reader.ReadBytes(4); // "RIFF"
            reader.ReadInt32(); // File size
            reader.ReadBytes(4); // "WAVE"
            reader.ReadBytes(4); // "fmt "
            int subchunk1Size = reader.ReadInt32();
            reader.ReadInt16(); // Audio format
            int channels = reader.ReadInt16();
            int sampleRate = reader.ReadInt32();
            reader.ReadInt32(); // Byte rate
            reader.ReadInt16(); // Block align
            int bitsPerSample = reader.ReadInt16();

            int extraBytes = subchunk1Size - 16;
            if (extraBytes > 0) reader.ReadBytes(extraBytes);

            // Read "data" chunk
            while (reader.ReadBytes(4) != new byte[] { 100, 97, 116, 97 }) // "data"
            {
                int chunkSize = reader.ReadInt32();
                reader.ReadBytes(chunkSize);
            }
            
            int dataSize = reader.ReadInt32();
            float[] audioData = new float[dataSize / (bitsPerSample / 8)];
            
            if (bitsPerSample == 16)
            {
                for (int i = 0; i < audioData.Length; i++)
                    audioData[i] = reader.ReadInt16() / 32768f;
            }
            else
            {
                Debug.LogError("Unsupported bit depth: " + bitsPerSample);
                return null;
            }

            AudioClip clip = AudioClip.Create("TTS_Audio", audioData.Length, channels, sampleRate, false);
            clip.SetData(audioData, 0);
            return clip;
        }
    }

    /// <summary>
    /// Writes WAV header information.
    /// </summary>
    private static void WriteWavHeader(BinaryWriter writer, AudioClip clip)
    {
        int sampleRate = clip.frequency;
        int channels = clip.channels;
        int bitsPerSample = 16;
        int byteRate = sampleRate * channels * bitsPerSample / 8;
        int blockAlign = channels * bitsPerSample / 8;
        int subchunk2Size = clip.samples * channels * bitsPerSample / 8;

        writer.Write(new char[] { 'R', 'I', 'F', 'F' });
        writer.Write(36 + subchunk2Size);
        writer.Write(new char[] { 'W', 'A', 'V', 'E' });
        writer.Write(new char[] { 'f', 'm', 't', ' ' });
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write((short)blockAlign);
        writer.Write((short)bitsPerSample);
        writer.Write(new char[] { 'd', 'a', 't', 'a' });
        writer.Write(subchunk2Size);
    }

    /// <summary>
    /// Writes WAV data.
    /// </summary>
    private static void WriteWavData(BinaryWriter writer, AudioClip clip)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        foreach (float sample in samples)
            writer.Write((short)(sample * 32767));
    }
}

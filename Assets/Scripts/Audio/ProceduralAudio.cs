using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine;

namespace BustAKlud
{
    /// <summary>
    /// Synthesises the sound effects the supplied recordings don't cover, and the background music: a
    /// harpsichord-and-celesta waltz in A minor over a quiet organ pad and a breath of wind.
    /// </summary>
    public static class ProceduralAudio
    {
        public const int SfxRate = 32000;
        public const int MusicRate = 22050;

        const int TableSize = 2048;
        static readonly float[] SineTable = BuildSineTable();

        static float[] BuildSineTable()
        {
            var table = new float[TableSize + 1];
            for (int i = 0; i <= TableSize; i++)
                table[i] = Mathf.Sin(i / (float)TableSize * Mathf.PI * 2f);
            return table;
        }

        /// <summary>Fast sine of a phase measured in cycles.</summary>
        static float Sin(float cycles)
        {
            float x = (cycles - Mathf.Floor(cycles)) * TableSize;
            int i = (int)x;
            float f = x - i;
            return SineTable[i] + (SineTable[i + 1] - SineTable[i]) * f;
        }

        static float Midi(int note) => 440f * Mathf.Pow(2f, (note - 69) / 12f);

        static AudioClip MakeClip(string name, float[] data, int rate)
        {
            var clip = AudioClip.Create(name, data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // ---------------------------------------------------------------- effects

        /// <summary>A bubbly "bloop" for popping.</summary>
        public static AudioClip Pop()
        {
            int n = (int)(SfxRate * 0.22f);
            var data = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SfxRate;
                float freq = Mathf.Lerp(900f, 360f, 1f - Mathf.Exp(-t * 22f));
                phase += freq / SfxRate;
                float env = Mathf.Exp(-t * 17f) * Mathf.Min(1f, t * 600f);
                float click = t < 0.006f ? (UnityEngine.Random.value - 0.5f) * 0.6f : 0f;
                data[i] = (Sin(phase) * 0.8f + Sin(phase * 2f) * 0.18f) * env + click;
            }

            return MakeClip("pop", data, SfxRate);
        }

        /// <summary>A little descending whistle for bubbles that fall.</summary>
        public static AudioClip Fall()
        {
            int n = (int)(SfxRate * 0.4f);
            var data = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SfxRate;
                float freq = Mathf.Lerp(780f, 210f, t / 0.4f) * (1f + 0.03f * Sin(t * 18f));
                phase += freq / SfxRate;
                float env = Mathf.Sin(Mathf.Clamp01(t / 0.4f) * Mathf.PI) * 0.7f;
                data[i] = Sin(phase) * env * 0.5f;
            }

            return MakeClip("fall", data, SfxRate);
        }

        public static AudioClip Click()
        {
            int n = (int)(SfxRate * 0.07f);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SfxRate;
                data[i] = Sin(t * 1500f) * Mathf.Exp(-t * 70f) * 0.7f;
            }

            return MakeClip("click", data, SfxRate);
        }

        public static AudioClip Hover()
        {
            int n = (int)(SfxRate * 0.05f);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SfxRate;
                data[i] = Sin(t * 2200f) * Mathf.Exp(-t * 110f) * 0.28f;
            }

            return MakeClip("hover", data, SfxRate);
        }

        /// <summary>Filtered noise rising and falling: swapping bubbles.</summary>
        public static AudioClip Whoosh()
        {
            int n = (int)(SfxRate * 0.26f);
            var data = new float[n];
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float cutoff = Mathf.Lerp(0.04f, 0.5f, Mathf.Sin(t * Mathf.PI * 0.5f));
                lp += (UnityEngine.Random.value * 2f - 1f - lp) * cutoff;
                data[i] = lp * Mathf.Sin(t * Mathf.PI) * 0.9f;
            }

            return MakeClip("whoosh", data, SfxRate);
        }

        /// <summary>A low grumbling stone slide for the ceiling dropping.</summary>
        public static AudioClip Rumble()
        {
            int n = (int)(SfxRate * 1.0f);
            var data = new float[n];
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SfxRate;
                lp += (UnityEngine.Random.value * 2f - 1f - lp) * 0.025f;
                float env = Mathf.Min(1f, t * 8f) * Mathf.Exp(-Mathf.Max(0f, t - 0.4f) * 3.5f);
                float tremolo = 0.75f + 0.25f * Sin(t * 14f);
                data[i] = (lp * 6f + Sin(t * 52f) * 0.5f + Sin(t * 79f) * 0.25f) * env * tremolo * 0.55f;
            }

            return MakeClip("rumble", data, SfxRate);
        }

        /// <summary>A rising bell arpeggio, with a sparkle on top.</summary>
        public static AudioClip LevelClear()
        {
            int[] notes = { 69, 72, 76, 81, 76, 81, 84 };
            float[] starts = { 0f, 0.11f, 0.22f, 0.33f, 0.52f, 0.63f, 0.78f };
            int n = (int)(SfxRate * 2.0f);
            var data = new float[n];
            for (int k = 0; k < notes.Length; k++)
                AddBell(data, SfxRate, starts[k], Midi(notes[k]), 0.4f, 3.2f);
            return MakeClip("levelclear", Normalize(data, 0.8f), SfxRate);
        }

        /// <summary>A sagging, wobbly descending sting.</summary>
        public static AudioClip GameOver()
        {
            int[] notes = { 64, 62, 60, 57 };
            float[] starts = { 0f, 0.42f, 0.84f, 1.26f };
            int n = (int)(SfxRate * 3.2f);
            var data = new float[n];

            for (int k = 0; k < notes.Length; k++)
            {
                float freq = Midi(notes[k] - 12);
                int begin = (int)(starts[k] * SfxRate);
                int length = (int)(SfxRate * (k == notes.Length - 1 ? 1.8f : 0.5f));
                float phase = 0f;
                for (int i = 0; i < length && begin + i < n; i++)
                {
                    float t = i / (float)SfxRate;
                    float vibrato = 1f + 0.015f * Sin(t * 6f);
                    float sag = k == notes.Length - 1 ? 1f - 0.12f * (t / 1.8f) : 1f;
                    phase += freq * vibrato * sag / SfxRate;
                    float saw = (phase - Mathf.Floor(phase)) * 2f - 1f;
                    float env = Mathf.Min(1f, t * 40f) * Mathf.Exp(-t * (k == notes.Length - 1 ? 1.6f : 5f));
                    data[begin + i] += (saw * 0.5f + Sin(phase) * 0.5f) * env * 0.5f;
                }
            }

            LowPass(data, 0.18f);
            return MakeClip("gameover", Normalize(data, 0.75f), SfxRate);
        }

        // ---------------------------------------------------------------- music

        static readonly int[][] Chords =
        {
            new[] { 57, 60, 64 },   // Am
            new[] { 57, 62, 65 },   // Dm
            new[] { 56, 59, 64 },   // E (G# B E)
            new[] { 57, 60, 65 }    // F
        };

        // 16 bars: A section then a variation. Index into Chords / bass roots per bar.
        static readonly int[] BarChord = { 0, 0, 2, 2, 0, 1, 2, 0, 0, 3, 2, 2, 0, 1, 2, 0 };
        static readonly int[] BarBass = { 45, 45, 52, 52, 45, 50, 52, 45, 45, 53, 52, 52, 45, 50, 52, 45 };

        // Melody as bar -> (beat, midi note, length in beats). Rests are simply omitted.
        static readonly float[][][] Melody =
        {
            new[] { new[] { 0f, 76f, 1f }, new[] { 1f, 72f, 0.5f }, new[] { 1.5f, 74f, 0.5f }, new[] { 2f, 72f, 1f } },
            new[] { new[] { 0f, 69f, 2.6f } },
            new[] { new[] { 0f, 71f, 1f }, new[] { 1f, 68f, 0.5f }, new[] { 1.5f, 71f, 0.5f }, new[] { 2f, 74f, 1f } },
            new[] { new[] { 0f, 76f, 2.6f } },
            new[] { new[] { 0f, 72f, 1f }, new[] { 1f, 76f, 0.5f }, new[] { 1.5f, 77f, 0.5f }, new[] { 2f, 76f, 1f } },
            new[] { new[] { 0f, 74f, 1f }, new[] { 1f, 77f, 1f }, new[] { 2f, 74f, 1f } },
            new[] { new[] { 0f, 68f, 1f }, new[] { 1f, 71f, 1f }, new[] { 2f, 74f, 1f } },
            new[] { new[] { 0f, 69f, 3f } },
            new[] { new[] { 0f, 81f, 1f }, new[] { 1f, 76f, 0.5f }, new[] { 1.5f, 77f, 0.5f }, new[] { 2f, 76f, 1f } },
            new[] { new[] { 0f, 72f, 1f }, new[] { 1f, 77f, 1f }, new[] { 2f, 72f, 1f } },
            new[] { new[] { 0f, 74f, 1f }, new[] { 1f, 71f, 0.5f }, new[] { 1.5f, 68f, 0.5f }, new[] { 2f, 71f, 1f } },
            new[] { new[] { 0f, 76f, 2f }, new[] { 2f, 75f, 0.5f }, new[] { 2.5f, 76f, 0.5f } },
            new[] { new[] { 0f, 76f, 1f }, new[] { 1f, 72f, 0.5f }, new[] { 1.5f, 74f, 0.5f }, new[] { 2f, 76f, 1f } },
            new[] { new[] { 0f, 77f, 1f }, new[] { 1f, 74f, 1f }, new[] { 2f, 69f, 1f } },
            new[] { new[] { 0f, 68f, 0.5f }, new[] { 0.5f, 71f, 0.5f }, new[] { 1f, 74f, 0.5f }, new[] { 1.5f, 76f, 0.5f }, new[] { 2f, 80f, 1f } },
            new[] { new[] { 0f, 81f, 2.6f } }
        };

        /// <summary>
        /// Renders the loop across several frames (a few milliseconds each) so starting the game never hitches.
        /// </summary>
        public static IEnumerator BuildMusic(Action<AudioClip> done)
        {
            const float bpm = 138f;
            float beat = 60f / bpm;
            float bar = beat * 3f;
            int bars = BarChord.Length;
            int total = (int)(bar * bars * MusicRate);
            var buffer = new float[total];
            var clock = Stopwatch.StartNew();

            for (int b = 0; b < bars; b++)
            {
                float barStart = b * bar;
                int[] chord = Chords[BarChord[b]];

                // Bass on the downbeat, chords strummed on beats two and three.
                AddPluck(buffer, barStart, Midi(BarBass[b]), 0.34f, 1.1f, 0.9f, true);
                for (int beatIndex = 1; beatIndex <= 2; beatIndex++)
                    for (int v = 0; v < chord.Length; v++)
                        AddPluck(buffer, barStart + beatIndex * beat + v * 0.012f, Midi(chord[v]), 0.1f, 0.5f, 3.5f, false);

                // Sustained organ pad under the chord.
                for (int v = 0; v < chord.Length; v++)
                    AddPad(buffer, barStart, bar, Midi(chord[v] - 12), 0.035f);

                foreach (float[] note in Melody[b])
                    AddCelesta(buffer, barStart + note[0] * beat, Midi((int)note[1]), 0.22f, note[2] * beat);

                if (clock.ElapsedMilliseconds > 5)
                {
                    clock.Restart();
                    yield return null;
                }
            }

            AddWind(buffer, 0.05f);
            LowPass(buffer, 0.62f);
            done(MakeClip("music", Normalize(buffer, 0.8f), MusicRate));
        }

        // Writes into the loop circularly so notes ringing past the end continue from the start.
        static void AddPluck(float[] buffer, float startTime, float freq, float gain, float seconds, float decay, bool bass)
        {
            int start = (int)(startTime * MusicRate);
            int length = (int)(seconds * MusicRate);
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)MusicRate;
                float phase = freq * t;
                float tone = bass
                    ? Sin(phase) + 0.45f * Sin(phase * 2f) + 0.12f * Sin(phase * 3f)
                    : Sin(phase) + 0.55f * Sin(phase * 2f) + 0.3f * Sin(phase * 3f) + 0.16f * Sin(phase * 4f);
                float env = Mathf.Min(1f, t * 900f) * Mathf.Exp(-t * decay * 2.2f);
                buffer[(start + i) % buffer.Length] += tone * env * gain;
            }
        }

        static void AddCelesta(float[] buffer, float startTime, float freq, float gain, float noteSeconds)
        {
            int start = (int)(startTime * MusicRate);
            int length = (int)(Mathf.Max(noteSeconds, 0.5f) * 1.5f * MusicRate);
            length = Mathf.Min(length, (int)(1.6f * MusicRate));
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)MusicRate;
                float vibrato = 1f + 0.004f * Sin(t * 5.5f);
                float phase = freq * t * vibrato;
                float tone = Sin(phase) + 0.32f * Sin(phase * 2f) + 0.12f * Sin(phase * 4.02f);
                float env = Mathf.Min(1f, t * 700f) * Mathf.Exp(-t * 3.2f);
                buffer[(start + i) % buffer.Length] += tone * env * gain;
            }
        }

        static void AddPad(float[] buffer, float startTime, float seconds, float freq, float gain)
        {
            int start = (int)(startTime * MusicRate);
            int length = (int)((seconds + 0.25f) * MusicRate);
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)MusicRate;
                float swell = Mathf.Min(1f, t * 6f) * Mathf.Clamp01((seconds + 0.25f - t) * 5f);
                float tone = Sin(freq * t) + 0.5f * Sin(freq * 2.003f * t) + 0.2f * Sin(freq * 3f * t);
                buffer[(start + i) % buffer.Length] += tone * swell * gain * (0.8f + 0.2f * Sin(t * 4.5f));
            }
        }

        static void AddWind(float[] buffer, float gain)
        {
            float lp = 0f;
            for (int i = 0; i < buffer.Length; i++)
            {
                float t = i / (float)MusicRate;
                float swell = 0.5f + 0.5f * Sin(t / (buffer.Length / (float)MusicRate) * 2f);
                lp += (UnityEngine.Random.value * 2f - 1f - lp) * 0.02f;
                buffer[i] += lp * 5f * gain * swell;
            }
        }

        static void AddBell(float[] data, int rate, float startTime, float freq, float gain, float decay)
        {
            int start = (int)(startTime * rate);
            int length = (int)(rate * 1.4f);
            for (int i = 0; i < length && start + i < data.Length; i++)
            {
                float t = i / (float)rate;
                float tone = Sin(freq * t) + 0.45f * Sin(freq * 2.01f * t) + 0.25f * Sin(freq * 3.97f * t) + 0.12f * Sin(freq * 5.4f * t);
                float env = Mathf.Min(1f, t * 900f) * Mathf.Exp(-t * decay);
                data[start + i] += tone * env * gain;
            }
        }

        static void LowPass(float[] data, float amount)
        {
            float state = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                state += (data[i] - state) * amount;
                data[i] = state;
            }
        }

        static float[] Normalize(float[] data, float peak)
        {
            float max = 0.0001f;
            for (int i = 0; i < data.Length; i++)
                max = Mathf.Max(max, Mathf.Abs(data[i]));
            float scale = peak / max;
            for (int i = 0; i < data.Length; i++)
                data[i] *= scale;
            return data;
        }
    }
}

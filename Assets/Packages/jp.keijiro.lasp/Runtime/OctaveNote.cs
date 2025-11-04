using UnityEngine;

namespace Lasp
{
    public struct OctaveNote
    {
        public string note;
        public int octave;
        public int cent;
        public int midiNum;
        public int noteFrequency;

        public override string ToString()
        {
            if (cent != 0)
            {
                return $"{note}{octave}+{cent}";
            }
            return $"{note}{octave}";
        }

        public static OctaveNote FromMidiNum(int midiNum)
        {
            return new OctaveNote
            {
                note = noteNameFromInt(midiNum),
                octave = (midiNum / 12) - 1,
                cent = 0,
                noteFrequency = Mathf.RoundToInt(440f * Mathf.Pow(2f, (midiNum - 69) / 12f)),
                midiNum = midiNum
            };
        }

        public static OctaveNote FromFrequency(float frequency)
        {
            if (frequency <= 0)
            {
                return new OctaveNote
                {
                    note = "",
                    octave = 0,
                    cent = 0,
                    noteFrequency = 0,
                    // TODO: is midi num 0 = 0hz?
                    midiNum = 0
                };
            }

            float midiNumFloat = 69 + 12 * Mathf.Log(frequency / 440f, 2);
            int midiNum = Mathf.RoundToInt(midiNumFloat);
            int cent = Mathf.RoundToInt((midiNumFloat - midiNum) * 100);

            return new OctaveNote
            {
                note = noteNameFromInt(midiNum),
                octave = (midiNum / 12) - 1,
                cent = cent,
                noteFrequency = Mathf.RoundToInt(440f * Mathf.Pow(2f, (midiNum - 69) / 12f)),
                midiNum = midiNum,
            };
        }

        public static int MidiNumFromFrequency(float frequency)
        {
            if (frequency <= 0) return 0;
            return Mathf.RoundToInt(69 + 12 * Mathf.Log(frequency / 440f, 2));
        }

        static string noteNameFromInt(int midiNum)
        {
            return (midiNum % 12) switch
            {
                0 => "C",
                1 => "C#",
                2 => "D",
                3 => "D#",
                4 => "E",
                5 => "F",
                6 => "F#",
                7 => "G",
                8 => "G#",
                9 => "A",
                10 => "A#",
                11 => "B",
                _ => ""
            };
        }
    }
}
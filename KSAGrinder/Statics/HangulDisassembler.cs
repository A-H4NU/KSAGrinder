using System;
using System.Linq;
using System.Text;

namespace KSAGrinder.Statics
{
    public static class HangulDisassembler
    {
        public const int HANGUL_JAMO_INITIAL_CONSONANT_COUNT = 19;
        public const int HANGUL_JAMO_MEDIAL_VOWEL_COUNT = 21;
        public const int HANGUL_JAMO_FINAL_CONSONANT_COUNT = 28;

        public const int HANGUL_SYLLABLES_START = 0xAC00;
        public const int HANGUL_SYLLABLES_END = 0xD7A3;

        public static readonly string[] HANGUL_INITIAL_CONSONANTS = { "ㄱ", "ㄲ", "ㄴ", "ㄷ", "ㄸ", "ㄹ", "ㅁ", "ㅂ", "ㅃ", "ㅅ", "ㅆ", "ㅇ", "ㅈ", "ㅉ", "ㅊ", "ㅋ", "ㅌ", "ㅍ", "ㅎ" };
        public static readonly string[] HANGUL_MEDIAL_VOWELS = { "ㅏ", "ㅐ", "ㅑ", "ㅒ", "ㅓ", "ㅔ", "ㅕ", "ㅖ", "ㅗ", "ㅗㅏ", "ㅗㅐ", "ㅗㅣ", "ㅛ", "ㅜ", "ㅜㅓ", "ㅜㅔ", "ㅜㅣ", "ㅠ", "ㅡ", "ㅡㅣ", "ㅣ" };
        public static readonly string[] HANGUL_FINAL_CONSONANTS = { "", "ㄱ", "ㄲ", "ㄱㅅ", "ㄴ", "ㄴㅈ", "ㄴㅎ", "ㄷ", "ㄹ", "ㄹㄱ", "ㄹㅁ", "ㄹㅅ", "ㄹㅅ", "ㄹㅌ", "ㄹㅍ", "ㄹㅎ", "ㅁ", "ㅂ", "ㅂㅅ", "ㅅ", "ㅆ", "ㅇ", "ㅈ", "ㅊ", "ㅋ", "ㅌ", "ㅍ", "ㅎ" };

        /// <summary>
        /// Transform each Hangul syllabls (U+AC00-U+D7A3) to its initial consonant(U+3131-U+314E)
        /// <para>
        /// ex) 닳a았b다 -> ㄷaㅇbㄷ
        /// </para>
        /// </summary>
        public static string ExtractInitialConsonants(string str)
        {
            StringBuilder sb = new();
            foreach (char c in str)
            {
                if (HANGUL_SYLLABLES_START <= c && c <= HANGUL_SYLLABLES_END)
                {
                    int index = c - HANGUL_SYLLABLES_START;
                    int initialIndex = index / (HANGUL_JAMO_MEDIAL_VOWEL_COUNT * HANGUL_JAMO_FINAL_CONSONANT_COUNT);
                    string initial = HANGUL_INITIAL_CONSONANTS[initialIndex];
                    sb.Append(initial);
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        ///     Transform each Hangul syllabls (U+AC00-U+D7A3) to its input order.
        /// <para>
        ///     ex) 닳a았b다 -> ㄷㅏㄹㅎaㅇㅏㅆbㄷㅏ
        /// </para>
        /// </summary>
        public static string Disassemble(string str)
        {
            StringBuilder sb = new();
            foreach (char c in str)
            {
                if (HANGUL_SYLLABLES_START <= c && c <= HANGUL_SYLLABLES_END)
                {
                    int index = c - HANGUL_SYLLABLES_START;
                    int initialIndex = index / (HANGUL_JAMO_MEDIAL_VOWEL_COUNT * HANGUL_JAMO_FINAL_CONSONANT_COUNT);
                    int medialIndex = (index % (HANGUL_JAMO_MEDIAL_VOWEL_COUNT * HANGUL_JAMO_FINAL_CONSONANT_COUNT)) / HANGUL_JAMO_FINAL_CONSONANT_COUNT;
                    int finalIndex = index % HANGUL_JAMO_FINAL_CONSONANT_COUNT;
                    string initial = HANGUL_INITIAL_CONSONANTS[initialIndex];
                    string medial = HANGUL_MEDIAL_VOWELS[medialIndex];
                    string final = HANGUL_FINAL_CONSONANTS[finalIndex];
                    sb.Append(initial + medial + final);
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Determine if all characters in <paramref name="str"/> are Hangul initial consonants in U+3131-U+314E.
        /// </summary>
        public static bool AreInitialConsonants(string str)
        {
            for (int i = 0; i < str.Length; i++)
            {
                if (!HANGUL_INITIAL_CONSONANTS.Contains(str.Substring(i, 1)))
                    return false;
            }
            return true;
        }
    }
}

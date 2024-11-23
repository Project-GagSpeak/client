using System.Collections.Generic;

namespace GagSpeak.Utils;

public static class GagPhonetics
{
    /// <summary>
    /// The list of all the phonemes that can be garbled (English List) (refer to the spreadsheet maybe)
    /// </summary>
    public static List<string> MasterListEN_UK = new List<string> {
        "b", "aʊ", "t", "k", "ə", "z", "ɔ", "ɹ", "s", "j", "u", "m",
        "f", "ɪ", "oʊ", "ʊ", "ɡ", "ɛ", "n", "eɪ", "d", "ɫ", "w", "i",
        "p", "ɑ", "ɝ", "θ", "v", "h", "æ", "ŋ", "ʃ", "ʒ", "aɪ", "dʒ",
        "tʃ", "ð", "ɔɪ", "ɪə" };

    public static List<string> MasterListEN_US = new List<string> {
        "p", "b", "m", "w", "f", "v", "θ", "ð", "t", "d", "s", "z",
        "n", "ɫ", "ɹ", "ʃ", "ʒ", "dʒ", "tʃ", "j", "k", "ɡ", "ŋ", "h",
        "eɪ", "ə", "ɔ", "æ", "i", "ɛ", "ɝ", "ɪə", "aɪ", "ɪ", "oʊ",
        "u", "ɑ", "ʊ", "aʊ", "ɔɪ" };

    public static List<string> MasterListSP_SPAIN = new List<string> {
        "a", "ɾ", "o", "n", "i", "k", "s", "β", "m", "ʎ", "ð", "d", "e",
        "t", "θ", "x", "ŋ", "g", "j", "ɲ", "l", "w", "z", "tʃ", "u",
        "r", "ɣ", "f", "b", "ʝ", "p", "ʃ" };

    public static List<string> MasterListSP_MEXICO = new List<string> {
        "a", "ɾ", "o", "n", "i", "k", "s", "β", "m", "ʎ", "ð", "d", "e",
        "t", "θ", "x", "ŋ", "g", "j", "ɲ", "l", "w", "z", "tʃ", "u", "r",
        "ɣ", "f", "b", "ʝ", "p", "ʃ" };

    public static List<string> MasterListFR_FRENCH = new List<string> {
        "a", "o", "k", "œ̃", "m", "ɔ", "ɑ̃", "p", "ʁ", "i", "t", "b", "ɔ̃",
        "u", "d", "ə", "ɔʁ", "z", "v", "ɛ", "e", "l", "s", "w", "ɛ̃", "f",
        "œ", "ʒ", "wa", "j", "y", "n", "ø", "g", "ks", "ɥ", "ʃ", "ɑ",
        "ɲ", "ʁː", "ŋ", "ʊ", "ε", "ɡ", "x", "ɪ", "aɪ" };

    public static List<string> MasterListFR_QUEBEC = new List<string> {
        "a", "o", "k", "œ̃", "m", "ɑ", "ɔ", "æ", "p", "ʁ", "i", "t", "s",
        "ɪ", "b", "õ", "ũ", "u", "ʊ", "d", "ə", "z", "v", "e", "l", "ɑ̃",
        "w", "ẽ", "ĩ", "f", "œ", "ʒ", "wa", "j", "y", "ʏ", "n", "ø", "g",
        "ã", "ks", "ɥ", "ʃ", "ɲ", "ʁː", "ɡ", "aɪ", "ŋ", "ɛ", "r", "ɔ̃",
        "ε", "x", "ʀ"};

    public static List<string> MasterListJP = new List<string> {
        "n", "a", "k", "g", "ɯ", "ɾ", "o", "p", "tɕ", "i", "ts", "ɴ",
        "m", "e", "dʑ", "d", "ɕ", "t", "b", "s", "ɰᵝ", "ɸ", "j", "z",
        "ç", "h", "v", "っ", "ッ", "ヮ", "ヶ", "ゎ"
    };
}

using GagSpeak.Utils;
using Lumina.Excel.Sheets;
using System.Text.RegularExpressions;

namespace GagSpeak;

// Most of these are from Lifestream for additional translations to be defined ahead of time.
internal static class GsLang
{
    public const string SymbolWard = "";
    public const string SymbolPlot = "";
    public const string SymbolApartment = "";
    public const string SymbolSubdivision = "";
    public static readonly (string Normal, string GameFont) Digits = ("0123456789", "");

    // All possible additoinal chambers entrance names, across all languages.
    internal static string[] AdditionalChambersEntrance =>
    [
        Svc.Data.GetExcelSheet<EObjName>().GetRow(2004353).Singular.ToDalamudString().ExtractText(),
            Regex.Replace(Svc.Data.GetExcelSheet<EObjName>().GetRow(2004353).Singular.ToDalamudString().ExtractText(), @"\[.*?\]", "")
    ];
    public static readonly string[] ConfirmChamberLeave = ["What would you like to do?"]; // SelectString, option we want is below.
    public static readonly string[] RejectChamberLeave = ["Nothing."];

    // All possible node names for entering a workshop, across all languages.
    internal static readonly string[] EnterWorkshop = ["Move to the company workshop", "地下工房に移動する", "移动到部队工房", "移動到部隊工房", "Die Gesellschaftswerkstätte betreten", "Aller dans l'atelier de compagnie", "지하공방으로 이동"];

    // All possible node names for apartment interactions, across all languages.
    public static readonly string[] GoToSpecifiedApartment = ["Go to specified apartment", "Go to speciz`fied apartment", "部屋番号を指定して移動（ハウスアピール確認）", "Eine bestimmte Wohnung betreten (Zweck der Unterkunft einsehen)", "移动到指定号码房间（查看房屋宣传标签）", "移動到指定號碼房間（查看房屋宣傳標籤）", "방 번호를 지정하여 이동(주택 정보 확인)", "Spécifier l'appartement où aller (Voir les attraits)"];
    public static readonly string[] EnterApartment = ["Enter", "よろしいですか？", "betreten?", "Aller dans l'appartement", "要移动到", "要移動到", "이동하시겠습니까?"];
    public static readonly string[] GoToMyApartment = ["Go to your apartment", "移动到自己的房间", "移動到自己的房間", "自分の部屋に移動する", "자신의 방으로 이동", "Aller dans votre appartement"]; //german missing

    public static readonly string[] ExitApartment = [ "Exit", "Ausgang" ]; // No actual label here, but is the name of the node
    public static readonly string[] RejectApartmentLeave = [ "Cancel", "Abbrechen", "Nein" ];

    internal static string[] ApartmentEntrance =>
    [
        Svc.Data.GetExcelSheet<EObjName>().GetRow(2007402).Singular.ToDalamudString().ExtractText(),
            Regex.Replace(Svc.Data.GetExcelSheet<EObjName>().GetRow(2007402).Singular.ToDalamudString().ExtractText(), @"\[.*?\]", "")
    ];

    // All possible node names revolving around entering homes.
    public static readonly string[] Entrance = [ "ハウスへ入る", "进入房屋", "進入房屋", "Eingang", "Entrée", "Entrance" ];
    public static readonly string[] ConfirmHouseEntrance = [ "「ハウス」へ入りますか？", "要进入这间房屋吗？", "要進入這間房屋嗎？", "Das Gebäude betreten?", " Das Gebäude betreten?", "Entrer dans la maison ?", "Enter the estate hall?" ];
    
    public static readonly string[] ConfirmHouseExit = [ "Leave the estate hall?", "Das Gebäude verlassen?"]; // yes | no option.

    public static readonly string[] ExitChambers = [ "Exit" ];

    public static readonly string[] DeepDungeonCoffer = [ "Treasure Coffer", "Schatztruhe" ];

}

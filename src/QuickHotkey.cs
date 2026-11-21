using System;
using System.Text.RegularExpressions;
namespace TranslateDesktop {
 public static class QuickHotkey {
  public static bool IsValid(string value){return value!=null&&value!="Win+L"&&Regex.IsMatch(value,@"^(Win\+|Ctrl\+Alt\+)[A-Z0-9]$");}
  public static bool Matches(string value,uint key,bool win,bool ctrl,bool alt,bool shift){if(!IsValid(value)||shift||key!=(uint)value[value.Length-1])return false;return value.StartsWith("Win+")?win&&!ctrl&&!alt:!win&&ctrl&&alt;}
 }
}

using System;
using System.IO;
class Target{static void Main(){File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"cold.txt"),"started");}}

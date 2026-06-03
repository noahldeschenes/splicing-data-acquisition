

using Utils;
using System.Diagnostics;


System.Console.WriteLine(System.IO.Directory.GetCurrentDirectory());
SplicerUtils.splicer.InitDriver(Process.GetCurrentProcess().Handle);

BackupUtils.Backup();

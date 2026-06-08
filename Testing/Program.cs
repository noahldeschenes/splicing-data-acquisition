
using Utils;
using System.Diagnostics;


SplicerUtils.splicer.InitDriver(Process.GetCurrentProcess().Handle);
SplicerUtils.QuitIfDisconnected();
SplicerUtils.splicer.StartVideo();



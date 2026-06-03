

using Utils;

SplicerUtils.InitializeAndLock();
BackupUtils.Backup();
SplicerUtils.splicer.Command("$UNLOCK");

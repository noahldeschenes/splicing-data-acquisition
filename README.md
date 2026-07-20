# RecordSplicingResults
RecordSplicingResults is a .NET command-line-interface written in C#, used primarily for backing up data/settings 
from FSM-100 series splicers.  
### Backup directory formats
There are two types of backups that RecordSplicingResults can handle. The first is a splice data backup, which 
collects the following for the most recent splice performed with a splicer.
- Prearc, warm splice, and cold images, each with the X and Y camera views
- A JSON with cleave angles, fiber angles, estimated loss, etc
- A binary backup for the splice mode used (e.g. FLEX-SMF V2)

The second type of backup is a single directory containing 300 binaries, one for each of the splicer's splice modes.
These are splice mode parameter backups, and can be used to return the splicer to a previous state.
### Destinations for backups
Both types of backups are sent to the local filesystem, as well as an AWS S3 bucket. For splice data backups, a single
backup is stored at a path of this format:
```
[Main directory for backups]\Splice data backups\[serial number] ([splicer name])\[mode title, e.g. FLEX-SMF]\[date]\[time]
```

Parameter backups have this path structure:
```
[Main directory for backups]\Splice mode parameter backups\[serial number] ([splicer name])\[date]
```
Note that this structure is the exact same for the local filesystem and for S3.
## Installation

## Usage


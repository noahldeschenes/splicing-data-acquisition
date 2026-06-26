using Spectre.Console;

var panel = new Panel("This software connects to Fujikura FSM-100 series splicers to migrate splice "+
            "data to the cloud. Please do not press any buttons on the splicer or open/close the cover while backups are in process.")
    .Header("FSM-100 series backup wizard (ver 1.1.0)");
  
AnsiConsole.Write(panel);

//Backend.
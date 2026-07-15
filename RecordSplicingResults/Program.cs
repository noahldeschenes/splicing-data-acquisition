
using RecordSplicingResults;
using System.Diagnostics;
using Spectre.Console;
using System;



static void SplicerConnected()
{   
    // <summary> Initializes the driver and checks if the splicer is connected. </summary>

    string message1 = "\n[red]ERROR[/]: Splicer disconnected. Try disconnecting and reconnecting the usb cable between "+
    "the splicer and the computer.";
    string message2 = "\n[red]ERROR[/]: Splicer still disconnected. Try turning the splicer off and back on.";
    string message3 = "\n[red]FATAL ERROR[/]: Splicer repeatedly not connecting. Drivers may be dysfunctional. Exiting...";

    string[] messages = {"", message1, message2, message3};

    foreach (string msg in messages)
    {
        if (Backend.splicer.ConnectionStatus) 
        {
            AnsiConsole.MarkupLine("Splicer connected...");
            return;
        }

        Backend.splicer.InitDriver(Process.GetCurrentProcess().Handle);
        if (msg == "") continue; // first iteration is just to check if we need to initialize the driver

        AnsiConsole.MarkupLine(msg);
        AnsiConsole.Prompt(
            new TextPrompt<string>("Press [green][[Enter]][/] to try again...")
                .AllowEmpty());
    }
}


static void StartConsole(string[] args)
{
    var panel = new Panel("This software connects to Fujikura FSM-100 series splicers to migrate splice "+
                "data to the cloud. Please do not press any buttons on the splicer or open/close the cover while backups are in process.")
        .Header("[blue]FSM-100 series backup wizard (ver 1.1.0)[/]");
    
    Console.Clear();
    AnsiConsole.Write(panel);



    while (true)
    {
        SplicerConnected();

        var choices = new[] {"Backup most recent splice", "Backup settings", "Open backups in files", "Quit"};

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("What would you like to do? (Use arrow keys to select and [green][[Enter]][/] to confirm).")
                .AddChoices(choices));
        
        switch (choice)
        {
            case "Backup most recent splice":
                Backend.BackupLastSplice(); 
                break;
            case "Backup settings":
                BackupUtils.Backup(BackupUtils.BACKUP_LOCATION); //change this
                break;
            case "Open backups in files":
                BackupUtils.OpenBackups();
                break;
            case "Quit":
                AnsiConsole.MarkupLine("\nQuitting...");
                Environment.Exit(0);
                break;
        }

        AnsiConsole.Prompt(
            new TextPrompt<string>(@"\nPress [green][[Enter]][/] to continue...")
                .AllowEmpty());

    }
}





try
{ 
    Console.CancelKeyPress += new ConsoleCancelEventHandler((sender, args) =>
    {
        Backend.currentBackupDirectory?.Delete(true);
        AnsiConsole.MarkupLine("[red]FATAL ERROR[/]: Program terminated unexpectedly.");
        Environment.Exit(0);
    }
    );
    StartConsole(args);
}
catch (Exception e)
{
    Backend.currentBackupDirectory?.Delete(true);
    AnsiConsole.MarkupLine($"[red]FATAL ERROR[/]: {e.Message}");
    Environment.Exit(0);
}



using Spectre.Console;
using System;

using static RecordSplicingResults.BackupService;
using static RecordSplicingResults.DataProcessor;
using static RecordSplicingResults.StatusHandler;
using static RecordSplicingResults.ParamService;
using static RecordSplicingResults.OutputHandler;




static void StartConsole()
{
    var panel = new Panel("This software connects to Fujikura FSM-100 series splicers to migrate splice "+
                "data to the cloud. Please do not press any buttons on the splicer or open/close the cover while backups are in process.")
        .Header("[blue]FSM-100 series backup wizard (ver 1.1.0)[/]");
    
    Console.Clear();
    AnsiConsole.Write(panel);



    while (true)
    {
        SplicerConnected();

        var choices = new[] {"Backup most recent splice", "Backup splices continuously", 
        "Backup settings", "Open backups in files", "Quit"};

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("What would you like to do? (Use arrow keys to select and [green][[Enter]][/] to confirm).")
                .AddChoices(choices));
        
        switch (choice)
        {
            case "Backup most recent splice":
                if (SplicerResting()) BackupLastSplice(); 
                break;
            case "Backup splices continuously":
                BackupSplicesContinuously();
                break;
            case "Backup settings":
                BackupParameters(BACKUP_LOCATION);
                break;
            case "Open backups in files":
                OpenBackups();
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
        currentBackupDirectory?.Delete(true);

        if (continuousModeOn) AnsiConsole.MarkupLine("\n[blue]Continuous backup stopped.[/]");
        else AnsiConsole.MarkupLine("[red]FATAL ERROR[/]: Program terminated unexpectedly.");
        
        Environment.Exit(0);
    }
    );

    StartConsole();
}
catch (Exception e)
{
    currentBackupDirectory?.Delete(true);
    AnsiConsole.MarkupLine($"[red]FATAL ERROR[/]: {e.Message}");
    Environment.Exit(0);
}


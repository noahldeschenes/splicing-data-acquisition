
using RecordSplicingResults;
using System.Diagnostics;
using Spectre.Console;
using System;

static void SplicerConnected()
{   
    // <summary> Initializes the driver and checks if the splicer is connected. </summary>
    
    Backend.splicer.InitDriver(Process.GetCurrentProcess().Handle); 
    if (Backend.splicer.ConnectionStatus) AnsiConsole.WriteLine("Splicer connected...");


    AnsiConsole.MarkupLine("[red]ERROR[/]: Splicer disconnected. Try disconnecting and reconnecting the usb cable between "+
    "the splicer and the computer.");
    AnsiConsole.Prompt(
        new TextPrompt<string>("Press [green][[[Enter]]][/] to try again...")
            .AllowEmpty());
    
    

    Backend.splicer.InitDriver(Process.GetCurrentProcess().Handle); 
    if (Backend.splicer.ConnectionStatus) AnsiConsole.WriteLine("Splicer connected...");

    
    AnsiConsole.MarkupLine("[red]ERROR[/]: Splicer still disconnected. Try turning the splicer off and back on.");
    AnsiConsole.Prompt(
        new TextPrompt<string>("Press [green][[[Enter]]][/] to try again...")
            .AllowEmpty());
    

    if (Backend.splicer.ConnectionStatus) AnsiConsole.WriteLine("Splicer connected...");


    AnsiConsole.MarkupLine("[red]FATAL ERROR[/]: Splicer repeatedly not connecting. Exiting...");
    Environment.Exit(0);           
}



var panel = new Panel("This software connects to Fujikura FSM-100 series splicers to migrate splice "+
            "data to the cloud. Please do not press any buttons on the splicer or open/close the cover while backups are in process.")
    .Header("[blue]FSM-100 series backup wizard (ver 1.1.0)[/]");
  
AnsiConsole.Write(panel);

while (true)
{
    SplicerConnected();

    var choices = new[] {"Backup most recent splice", "Backup settings", "Open backups in files", "Quit"};

    var choice = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("What would you like to do?")
            .AddChoices(choices));
    
    if (choice == "Backup most recent splice")
    {
        
    }

}

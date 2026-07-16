using System;
using System.Diagnostics;
using Spectre.Console;

using static RecordSplicingResults.OutputHandler;

namespace RecordSplicingResults
{
    static class StatusHandler
    {
        public static bool SplicerResting(bool verbose=true)
        {
            // <summary>Checks if the splicer is at a valid state for queries.</summary>

            string currentStatus = splicer.CommandAndReceiveText("=FUNCSTAT");
            if (currentStatus != "IDLE" && currentStatus != "ERRFIN" && currentStatus != "NOFIN") 
            {
                if (verbose)
                {
                    AnsiConsole.WriteLine($"ERROR: Splicer is not at the READY or FINISH state (currently at '{currentStatus}' state).");
                }
                return false;
            }
            else
            {
                return true;
            }
        }

        public static void SplicerConnected(bool verbose=true)
        {   
            // <summary> Initializes the driver and checks if the splicer is connected. </summary>

            string message1 = "\n[red]ERROR[/]: Splicer disconnected. Try disconnecting and reconnecting the usb cable between "+
            "the splicer and the computer.";
            string message2 = "\n[red]ERROR[/]: Splicer still disconnected. Try turning the splicer off and back on.";
            string message3 = "\n[red]FATAL ERROR[/]: Splicer repeatedly not connecting. Drivers may be dysfunctional. Exiting...";

            string[] messages = {"", message1, message2, message3};

            foreach (string msg in messages)
            {
                if (splicer.ConnectionStatus) 
                {   
                    if (verbose) AnsiConsole.MarkupLine("Splicer connected...");
                    return;
                }

                splicer.InitDriver(Process.GetCurrentProcess().Handle);
                if (msg == "") continue; // first iteration is just to check if we need to initialize the driver

                AnsiConsole.MarkupLine(msg);
                AnsiConsole.Prompt(
                    new TextPrompt<string>("Press [green][[Enter]][/] to try again...")
                        .AllowEmpty());
            }
        }
    }
}
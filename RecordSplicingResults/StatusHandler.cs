using System;
using System.Diagnostics;
using Spectre.Console;

using static RecordSplicingResults.OutputHandler;

namespace RecordSplicingResults
{
    /// <summary>
    /// A handler class that deals with the splicer's status (connected vs. disconnected,
    /// idle vs busy, etc).
    /// </summary>
    internal static class StatusHandler
    {
        /// <summary>
        /// Checks if the splicer is at a valid state for queries.
        /// </summary>
        /// <param name="verbose">Boolean representing whether this method should print text.</param>
        /// <returns>A boolean representing if the splicer is resting.</returns>
        internal static bool SplicerResting(bool verbose=true)
        { 

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

        /// <summary>
        /// Gives the user troubleshooting instructions on how to connect the splicer, if disconnected.
        /// </summary>
        /// <param name="verbose">Boolean representing if a successful connection is printed.</param>
        internal static void TryConnect(bool verbose=true)
        {   

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
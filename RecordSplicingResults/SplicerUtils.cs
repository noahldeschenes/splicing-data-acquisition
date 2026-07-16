
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Spectre.Console;
using System.Threading;



namespace RecordSplicingResults
{
    /*
        Folder: date
        Files inside: JSON, images, errors


        JSON format: Splicer info, Relevant splice settings, Volatile memory splice data, 
        Non-volatile memory splice data

    */


    class SplicerUtils
    {

        


        public static readonly string RECORDS_DIRECTORY_PATH = @"C:\Users\noah.deschenes\Documents\Records"; // TODO: find directory
        public const int NUM_OF_MODES = 300; 
        public const string NAK = "\x15"; // ASCII code for NAK
        public static UsbFsm100ServerClass splicer = new();

        public static bool continuousModeOn = false;

        

        
        

        

          
    }
}
    

    


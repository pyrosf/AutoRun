﻿using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Drawing.Imaging;
using Emgu.CV;
using Emgu.CV.OCR;
using Emgu.CV.Structure;
using System.Collections.Concurrent;
using AutoHotkey.Interop;
using System.Diagnostics;


public static class Input
{
    [DllImport("user32.dll")]
    static extern bool GetCursorPos(out POINT point);


    [DllImport("user32.dll")]
    static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);


    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int x;
        public int y;
    }

    public static POINT GetMousePosition()
    {
        POINT pos;
        GetCursorPos(out pos);
        return pos;
    }
    public static Color GetPixelColor(int x, int y)
    {
        IntPtr desktopPtr = GetDC(IntPtr.Zero);
        uint color = GetPixel(desktopPtr, x, y);
        ReleaseDC(IntPtr.Zero, desktopPtr);

        // Extract the RGB components from the color value
        byte red = (byte)(color & 0xFF);
        byte green = (byte)((color >> 8) & 0xFF);
        byte blue = (byte)((color >> 16) & 0xFF);

        return Color.FromArgb(red, green, blue);
    }
    public static string ColorToHex(Color color)
    {
        return "#" + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
    }


}  // This is my DLL and mouse pointer code

class Program
{
    static string _filePath = @"C:\Users\Public\Daybreak Game Company\Installed Games\EverQuest\Logs\eqlog_Ndiinea_vaniki.txt";
    static long _fileSize = 0;
    static bool firsttime = true;
    static ConcurrentQueue<string> _lineQueue = new ConcurrentQueue<string>();

    public static int RateOfChange(int[] Values)
    {
        if (Values.Length < 10)
        { return 0; }
        int TotalTime = 5;
        int SampleTime = TotalTime / Values.Length;
        int SumRateOfChange = 0;
        for (int i = 0; i < 9; i++)
        {
            int rateofchange = (Values[i + 1] - Values[i] / SampleTime);
            SumRateOfChange += rateofchange;

        }
        int AvgRate = SumRateOfChange / 9;



        return AvgRate;
    }


    static void PrintNewLines()
    {
        // Open the log file
        using (var fileStream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            // Set the file position to the end of the previous file contents
            fileStream.Seek(_fileSize, SeekOrigin.Begin);

            // Read the new lines that were added to the file
            using (var streamReader = new StreamReader(fileStream))
            {
                string line;
                while ((line = streamReader.ReadLine()) != null)
                {

                    Console.WriteLine(line);
                    if (firsttime)
                    { continue; }
                    _lineQueue.Enqueue(line);  // this prevents locking and ensures each line is put in the queue to be processed without impacting the file

                }

                // Update the file size to the current position in the file
                _fileSize = fileStream.Position;
                firsttime = false;
            }
        }
    }

    [DllImport("user32.dll")]
    public static extern bool GetAsyncKeyState(int button);

    public static int CaptureScreenLocation(int x1, int y1, int x2, int y2)
    {
        

        // Calculate width and height of the rectangle
        int width = Math.Abs(x2 - x1);
        int height = Math.Abs(y2 - y1);

        Bitmap screenCapture = CaptureScreenRegion(x1, y1, width, height);
        Image<Bgr, byte> emguImage = screenCapture.ToImage<Bgr, byte>();
        string recognizedText = PerformOCR(emguImage);
        recognizedText = recognizedText.Replace("%", "");
        int returnvalue = -1;
        try
        {
            returnvalue = int.Parse(recognizedText);
        }
        catch
        {
            returnvalue = -1;
        }




        return returnvalue;
    }
   
    
    public static bool IsMouseButtonPressed(MouseButton button)
    {
        return GetAsyncKeyState((int)button);
    }

    public enum MouseButton
    {
        LeftMouseButton = 0x01,
        RightMouseButton = 0x02,
        MiddleMouseButton = 0x04
    }

    class DataObject
    {
        public int X { get; set; }
        public int Y { get; set; }
        public string Hex { get; set; }
        public int HP { get; set; }
        public string Notes { get; set; }
        public string Description { get; set; }
    }

    class Stats
    {
        public int Target_HP { get; set; }
        public int Target_HP1 { get; set; }
        public int Target_HP2 { get; set; }
        public int Target_HP3 { get; set; }
        public int Target_HP4 { get; set; }
        public int Target_HP5 { get; set; }
        public int Target_HP6 { get; set; }
        public int My_HP { get; set; }
        public int My_MP { get; set; }
        public int Casting { get; set; }
        public int Party1 { get; set; }
        public int Party2 { get; set; }
        public int Party3 { get; set; }
        public int Party4 { get; set; }
        public int Party5 { get; set; }
        public int TargetOfTarget { get; set; }
        public int MyLevel { get; set; }
        public int Sitting { get; set;}



    }
    static List<DataObject> ReadCSVFile(string filePath)
    {
        List<DataObject> dataObjects = new List<DataObject>();

        try
        {
            using (StreamReader reader = new StreamReader(filePath))
            {
                string headerLine = reader.ReadLine();
                if (headerLine != null)
                {
                    string[] headers = headerLine.Split(',');

                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        if (!string.IsNullOrEmpty(line))
                        {
                            string[] values = line.Split(',');

                            DataObject dataObject = new DataObject();

                            // Assuming the CSV columns are in the same order as the object properties
                            for (int i = 0; i < values.Length; i++)
                            {
                                string value = values[i];
                                switch (headers[i].ToLower())
                                {
                                    case "x":
                                        dataObject.X = int.Parse(value);
                                        break;
                                    case "y":
                                        dataObject.Y = int.Parse(value);
                                        break;
                                    case "hex":
                                        dataObject.Hex = value;
                                        break;
                                    case "hp":
                                        dataObject.HP = int.Parse(value);
                                        break;
                                    case "notes":
                                        dataObject.Notes = value;
                                        break;
                                    case "description":
                                        dataObject.Description = value;
                                        break;
                                    default:
                                        // Handle unknown headers/columns
                                        break;
                                }
                            }

                            dataObjects.Add(dataObject);
                        }
                    }
                }
            }
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine($"File '{filePath}' not found.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }

        return dataObjects;
    }
    static Bitmap CaptureScreenRegion(int x, int y, int width, int height)
    {
        Bitmap bitmap = new Bitmap(width, height);
        using (Graphics g = Graphics.FromImage(bitmap))
        {
            g.CopyFromScreen(x, y, 0, 0, new Size(width, height));
        }
        return bitmap;
    }

    static string PerformOCR(Image<Bgr, byte> image)
    {
        // Initialize Tesseract OCR engine
        Tesseract ocr = new Tesseract(@"C:\EQItems", "eng", OcrEngineMode.Default);

        // Load image into OCR engine
        ocr.SetImage(image);

        // Perform OCR
        ocr.Recognize();

        // Get recognized text
        string text = ocr.GetUTF8Text();
        ocr.Dispose();
        return text;
    }

    static void Main()
    {
        



        // setting up AutoHotKey
        var ahk = AutoHotkeyEngine.Instance;
        ahk.ExecRaw("SetKeyDelay, 2");

        // Modes!

        bool AutoTarget = false;
        bool AutoSit = false;
        bool Auction = false;

        List<int> Target1 = new List<int> { 1387, 384, 1413, 396 };
        List<int> HpStaMp = new List<int> { 1638, 406, 1676, 471 };
        List<int> Party1 = new List<int> { 1638, 482, 1669, 496 };
        List<int> Party2 = new List<int> { 1638, 513, 1669, 527 };
        List<int> Party3 = new List<int> { 1638, 544, 1669, 560 };
        List<int> Party4 = new List<int> { 1638, 570, 1669, 591 };
        List<int> Party5 = new List<int> { 1638, 609, 1669, 623 };
        List<int> TargetOfTarget = new List<int> { 1503, 375, 1527, 388 };
        List<int> MyLevel = new List<int> { 1534, 641, 1553, 654 };  // does not get level 1
        List<int> TargetBox1 = new List<int> { 1429, 509, 1454, 524 };
        List<int> TargetBox2 = new List<int> { 1429, 544, 1454, 559 };
        List<int> TargetBox3 = new List<int> { 1429, 579, 1454, 594 };
        List<int> TargetBox4 = new List<int> { 1429, 614, 1454, 629 };
        List<int> TargetBox5 = new List<int> { 1429, 647, 1454, 662 };
        List<int> TargetBox6 = new List<int> { 1429, 680, 1454, 695 };

        Random random = new Random();
        string filePath2 = @"C:\temp\EQTestFiles\Data.csv";
        List<DataObject> dataObjects = ReadCSVFile(filePath2);
        
        List<DataObject> ActiveCasting = new List<DataObject>(); // keep
        List<DataObject> Sitting = new List<DataObject>(); // keep

        Stats EQStats = new Stats();
        // Read in the Data File

        foreach (DataObject obj in dataObjects)
        {
            Console.WriteLine($"X: {obj.X}, Y: {obj.Y}, Hex: {obj.Hex}, HP: {obj.HP}, Notes: {obj.Notes}, Description: {obj.Description}");
            
            if (obj.Description == "Casting") { ActiveCasting.Add(obj); }
            if (obj.Description == "Sitting") { Sitting.Add(obj); }

        }
       

        bool stopPrinting = false;

        bool OutOfRange = false;
        bool ManaRegen = false;
        bool backup = false;
        Int64 counter = 0;


        Thread printThread = new Thread(() =>
        {
        while (!stopPrinting)
        {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                foreach (DataObject obj in ActiveCasting)
            {
                Color pixelColor = Input.GetPixelColor(obj.X, obj.Y);
                string hexColor = Input.ColorToHex(pixelColor);
                if (hexColor == obj.Hex)
                {
                    EQStats.Casting = 1;
                    
                }
                if (hexColor != obj.Hex)
                {
                    EQStats.Casting = -1;
                }

            }
                foreach (DataObject obj in Sitting)
                {
                    Color pixelColor = Input.GetPixelColor(obj.X, obj.Y);
                    string hexColor = Input.ColorToHex(pixelColor);
                    if (hexColor == obj.Hex)
                    {
                        EQStats.Sitting = 1;

                    }
                    if (hexColor != obj.Hex)
                    {
                        EQStats.Sitting = -1;
                    }

                }
                // Get my stats More Complex, cant use the simplified system

                int x1 = HpStaMp[0]; // x-coordinate of top-left corner
            int y1 = HpStaMp[1]; // y-coordinate of top-left corner
            int x2 = HpStaMp[2]; // x-coordinate of bottom-right corner
            int y2 = HpStaMp[3]; // y-coordinate of bottom-right corner

            // Calculate width and height of the rectangle
            int width = Math.Abs(x2 - x1);
            int height = Math.Abs(y2 - y1);

            Bitmap screenCapture = CaptureScreenRegion(x1, y1, width, height);
            Image<Bgr, byte> emguImage = screenCapture.ToImage<Bgr, byte>();
            string recognizedText = PerformOCR(emguImage);
            recognizedText = recognizedText.Replace("%", "");
            

            string[] subregconizedtext = recognizedText.Split(Environment.NewLine);
            try
            {
                if (subregconizedtext[0].Contains("100"))
                {
                    subregconizedtext[0] = "100";
                }
                else
                {
                    subregconizedtext[0] = subregconizedtext[0].Substring(0, 2);
                }
                if (subregconizedtext[2].Contains("100"))
                {
                    subregconizedtext[2] = "100";
                }
                else
                {
                    subregconizedtext[2] = subregconizedtext[2].Substring(0, 2);
                }


                EQStats.My_HP = int.Parse(subregconizedtext[0]);
                EQStats.My_MP = int.Parse(subregconizedtext[2]);
                
                

            } catch {
                EQStats.My_HP = -1;
                EQStats.My_MP = -1;
            }
            // Add Check to stop casting here  --  Might be able to get current target
            //if (EQStats.Casting == 1)  // Check to see if currently casting, if so do nothing
            //{
            //    Thread.Sleep(500);
            //    continue;
            //}

            //x1 = Target1[0]; // x-coordinate of top-left corner
            //y1 = Target1[1]; // y-coordinate of top-left corner
            //x2 = Target1[2]; // x-coordinate of bottom-right corner
            //y2 = Target1[3]; // y-coordinate of bottom-right corner

            //// Calculate width and height of the rectangle
            //width = Math.Abs(x2 - x1);
            //height = Math.Abs(y2 - y1);

            //screenCapture = CaptureScreenRegion(x1, y1, width, height);
            //emguImage = screenCapture.ToImage<Bgr, byte>();
            //recognizedText = PerformOCR(emguImage);
            //recognizedText = recognizedText.Replace("%", "");

            //    try
            //    {
            //        EQStats.Target_HP1 = int.Parse(recognizedText);
            //    }
            //    catch
            //    {
            //        EQStats.Target_HP1 = -1;
            //    }

                ///  Party 1
                ///  
                EQStats.Target_HP = CaptureScreenLocation(Target1[0], Target1[1], Target1[2], Target1[3]);
                EQStats.Target_HP1 = CaptureScreenLocation(TargetBox1[0], TargetBox1[1], TargetBox1[2], TargetBox1[3]);
                EQStats.Target_HP2 = CaptureScreenLocation(TargetBox2[0], TargetBox2[1], TargetBox2[2], TargetBox2[3]);
                EQStats.Target_HP3 = CaptureScreenLocation(TargetBox3[0], TargetBox3[1], TargetBox3[2], TargetBox3[3]);
                EQStats.Target_HP4 = CaptureScreenLocation(TargetBox4[0], TargetBox4[1], TargetBox4[2], TargetBox4[3]);
                EQStats.Target_HP5 = CaptureScreenLocation(TargetBox5[0], TargetBox5[1], TargetBox5[2], TargetBox5[3]);
                EQStats.Target_HP6 = CaptureScreenLocation(TargetBox6[0], TargetBox6[1], TargetBox6[2], TargetBox6[3]);
                //// end party 1    
                EQStats.Party1 = CaptureScreenLocation(Party1[0], Party1[1], Party1[2], Party1[3]);
                EQStats.Party2 = CaptureScreenLocation(Party2[0], Party2[1], Party2[2], Party2[3]);
                EQStats.Party3 = CaptureScreenLocation(Party3[0], Party3[1], Party3[2], Party3[3]);
                EQStats.Party4 = CaptureScreenLocation(Party4[0], Party4[1], Party4[2], Party4[3]);
                EQStats.Party5 = CaptureScreenLocation(Party5[0], Party5[1], Party5[2], Party5[3]);
                if (EQStats.MyLevel == 0)
                {
                    EQStats.MyLevel = CaptureScreenLocation(MyLevel[0], MyLevel[1], MyLevel[2], MyLevel[3]);
                }
                //EQStats.TargetOfTarget = CaptureScreenLocation(TargetOfTarget[0], TargetOfTarget[1], TargetOfTarget[2], TargetOfTarget[3]);
                // compress the above to a single line
                ////////////////////////   Add new screen reading code here!!! ////////////////////////////


                ////////////////////////   End Screen Reading Code ////////////////////////////////////////
                ///

                watch.Stop();
                Console.Clear();
                Console.WriteLine($"Target_HP: {EQStats.Target_HP}");
                Console.WriteLine($"Target of Target: {EQStats.TargetOfTarget}");
                Console.WriteLine($"Target_HP1: {EQStats.Target_HP1}");
                Console.WriteLine($"Target_HP2: {EQStats.Target_HP2}");
                Console.WriteLine($"Target_HP3: {EQStats.Target_HP3}");
                Console.WriteLine($"Target_HP4: {EQStats.Target_HP4}");
                Console.WriteLine($"Target_HP5: {EQStats.Target_HP5}");
                Console.WriteLine($"Target_HP6: {EQStats.Target_HP6}");
                Console.WriteLine($"My_HP: {EQStats.My_HP}");
                Console.WriteLine($"My_MP: {EQStats.My_MP}");
                Console.WriteLine($"My Level: {EQStats.MyLevel}");
                Console.WriteLine($"Casting: {EQStats.Casting}");
                Console.WriteLine($"Party 1: {EQStats.Party1}");
                Console.WriteLine($"Party 2: {EQStats.Party2}");
                Console.WriteLine($"Party 3: {EQStats.Party3}");
                Console.WriteLine($"Party 4: {EQStats.Party4}");
                Console.WriteLine($"Party 5: {EQStats.Party5}");
                Console.WriteLine($"Sitting: {EQStats.Sitting}");
                Console.WriteLine($"Execution Time: {watch.ElapsedMilliseconds} ms");
                counter++;
                Thread.Sleep(100);
                
            }
        });


        Thread ReadThread = new Thread(() =>
        {
            while (!stopPrinting)
            {
                // Print out the current contents of the file
                PrintNewLines();


                // Create a new FileSystemWatcher object to monitor the log file
                using (var watcher = new FileSystemWatcher(Path.GetDirectoryName(_filePath), Path.GetFileName(_filePath)))
                {
                    // Set the notification filter to watch for changes in LastWrite time
                    watcher.NotifyFilter = NotifyFilters.LastWrite;

                    // Start watching for changes
                    watcher.EnableRaisingEvents = true;

                    // Loop indefinitely
                    while (true)
                    {
                        // Wait for a change notification
                        var result = watcher.WaitForChanged(WatcherChangeTypes.Changed);

                        // Check if the file was changed
                        if (result.ChangeType == WatcherChangeTypes.Changed)
                        {
                            // Print out the new lines that were added to the file
                            PrintNewLines();
                        }
                    }
                }

            }

        });  // This reads the EQ Log

        printThread.Start();
        ReadThread.Start();









        // Wait for the user to press Enter to stop printing
        Console.ReadLine();

        stopPrinting = true; // Set the flag to stop the printing loop
        printThread.Join(); // Wait for the printThread to finish
        ReadThread.Join();
        Console.WriteLine("Printing stopped.");
    }
}

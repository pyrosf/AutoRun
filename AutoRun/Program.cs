using System;
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

    static void SaveBitmapToFile(Bitmap bitmap, string filePath)
    {
        bitmap.Save(filePath, ImageFormat.Png); // Save as PNG format





    }
    

    

    public static string GetCurrentFValue(int Number)
    {
        switch (Number)
        {
            case 0:
                return "{F1}";

            case 1:
                return "{F2}";

            case 2:
                return "{F3}";

            case 3:
                return "{F4}";

            case 4:
                return "{F5}";

            case 5:
                return "{F6}";
        }
        return "{F1}";
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
        // Debug Section
        //string filePath = @$"C:\EQItems\{x1}_{y1}.jpg"; // Path to save the file
        //SaveBitmapToFile(screenCapture, filePath);



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
        public bool Sitting { get; set; }
        public bool following { get; set; }
        public bool Combat { get; set; }
        public bool Recovery { get; set; }



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
    static void RandomMove()
    {
        var ahk = AutoHotkeyEngine.Instance;
        ahk.ExecRaw("SetKeyDelay, 2");
        // Create an instance of the Random class
        Random random = new Random();

        // Generate a random number between 0 and 2 (inclusive)
        int randomNumber = random.Next(4);

        // Use a switch statement or if-else statements to perform actions based on the random number
        switch (randomNumber)
        {
            case 0:
                // Action 1
                ahk.ExecRaw("Send {s down}\r\nSleep 100\r\nSend {s up}");
                // TODO: Add your code for Action 1 here
                break;
            case 1:
                // Action 2
                ahk.ExecRaw("Send {d down}\r\nSleep 100\r\nSend {d up}");
                // TODO: Add your code for Action 2 here
                break;
            case 2:
                // Action 3
                ahk.ExecRaw("Send {s down}\r\nSleep 100\r\nSend {s up}");
                // TODO: Add your code for Action 3 here
                break;
            case 3:
                // Action 4
                ahk.ExecRaw("Send {d down}\r\nSleep 100\r\nSend {d up}");
                // TODO: Add your code for Action 3 here
                break;
        }
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
        bool buffing = false;
        Queue<string> notFoundImages = new Queue<string>();
        string CurrentBuffTarget = null;

        List<int> Target1 = new List<int> { 1387, 386, 1413, 398 };
        List<int> HpStaMp = new List<int> { 1638, 408, 1676, 473 };
        List<int> Party1 = new List<int> { 1638, 483, 1669, 500 };
        List<int> Party2 = new List<int> { 1638, 515, 1669, 529 };
        List<int> Party3 = new List<int> { 1638, 546, 1669, 562 };
        List<int> Party4 = new List<int> { 1638, 572, 1669, 593 };
        List<int> Party5 = new List<int> { 1638, 611, 1669, 625 };
        List<int> TargetOfTarget = new List<int> { 1503, 377, 1527, 390 };
        List<int> MyLevel = new List<int> { 1534, 642, 1553, 655 };  // does not get level 1
        List<int> TargetBox1 = new List<int> { 1429, 511, 1454, 526 };
        List<int> TargetBox2 = new List<int> { 1429, 546, 1454, 561 };
        List<int> TargetBox3 = new List<int> { 1429, 581, 1454, 598 };
        List<int> TargetBox4 = new List<int> { 1429, 616, 1454, 633 };
        List<int> TargetBox5 = new List<int> { 1429, 649, 1454, 666 };
        List<int> TargetBox6 = new List<int> { 1429, 682, 1454, 697 };
        List<int> BuffBox = new List<int> { 1285, 395, 1523, 465 };

        Random random = new Random();
        string filePath2 = @"C:\temp\EQTestFiles\Data.csv";
        List<DataObject> dataObjects = ReadCSVFile(filePath2);

        List<DataObject> ActiveCasting = new List<DataObject>(); // keep
        //List<DataObject> Sitting = new List<DataObject>();
        //List<DataObject> Standing = new List<DataObject>();
        //List<DataObject> Recovery = new List<DataObject>();// keep
        //List<DataObject> Combat = new List<DataObject>();

        Stats EQStats = new Stats();
        // Read in the Data File

        foreach (DataObject obj in dataObjects)
        {
            Console.WriteLine($"X: {obj.X}, Y: {obj.Y}, Hex: {obj.Hex}, HP: {obj.HP}, Notes: {obj.Notes}, Description: {obj.Description}");

            if (obj.Description == "Casting") { ActiveCasting.Add(obj); }
            //if (obj.Description == "Sitting") { Sitting.Add(obj); }
            //if (obj.Description == "Recovery") { Sitting.Add(obj); }
            //if (obj.Description == "Standing") { Sitting.Add(obj); }
            //if (obj.Description == "Combat") { Sitting.Add(obj); }
        }


        bool stopPrinting = false;

        bool OutOfRange = false;
        bool ManaRegen = false;
        bool backup = false;


        Thread Read1 = new Thread(() =>
        {
            while (!stopPrinting)
            {
                EQStats.Target_HP = CaptureScreenLocation(Target1[0], Target1[1], Target1[2], Target1[3]);
                EQStats.Target_HP1 = CaptureScreenLocation(TargetBox1[0], TargetBox1[1], TargetBox1[2], TargetBox1[3]);
                EQStats.Target_HP2 = CaptureScreenLocation(TargetBox2[0], TargetBox2[1], TargetBox2[2], TargetBox2[3]);
                EQStats.Target_HP3 = CaptureScreenLocation(TargetBox3[0], TargetBox3[1], TargetBox3[2], TargetBox3[3]);
                EQStats.Target_HP4 = CaptureScreenLocation(TargetBox4[0], TargetBox4[1], TargetBox4[2], TargetBox4[3]);
                EQStats.Target_HP5 = CaptureScreenLocation(TargetBox5[0], TargetBox5[1], TargetBox5[2], TargetBox5[3]);
                EQStats.Target_HP6 = CaptureScreenLocation(TargetBox6[0], TargetBox6[1], TargetBox6[2], TargetBox6[3]);


                Thread.Sleep(500);
            }
        });

        Thread Read2 = new Thread(() =>
        {
            while (!stopPrinting)
            {
                EQStats.Party1 = CaptureScreenLocation(Party1[0], Party1[1], Party1[2], Party1[3]);
                EQStats.Party2 = CaptureScreenLocation(Party2[0], Party2[1], Party2[2], Party2[3]);
                EQStats.Party3 = CaptureScreenLocation(Party3[0], Party3[1], Party3[2], Party3[3]);
                EQStats.Party4 = CaptureScreenLocation(Party4[0], Party4[1], Party4[2], Party4[3]);
                EQStats.Party5 = CaptureScreenLocation(Party5[0], Party5[1], Party5[2], Party5[3]);


                Thread.Sleep(500);
            }
        });


        Thread Read3 = new Thread(() =>
        {
            while (!stopPrinting)
            {
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



                }
                catch
                {
                    EQStats.My_HP = -1;
                    EQStats.My_MP = -1;
                }
                Thread.Sleep(500);
            }
        });

        





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
                    Console.WriteLine($"following: {EQStats.following}");
                    Console.WriteLine($"buffing?: {buffing}");
                    Console.WriteLine($"Combat: {EQStats.Combat}");
                    Console.WriteLine($"Execution Time: {watch.ElapsedMilliseconds} ms");
                    if (notFoundImages.Count > 0)
                    {
                        Console.WriteLine($"Not Found Buffs:" + notFoundImages.Peek());
                    }

                    Thread.Sleep(500);

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

            Thread QueueThread = new Thread(() =>
            {
                while (!stopPrinting)
                {
                    foreach (string Qline in _lineQueue)
                    {
                        string queueline = "";
                        bool isRemoved = _lineQueue.TryDequeue(out queueline);
                        string line = Qline.Substring(27);
                        if (line.Contains("*WARNING*"))
                        {
                            EQStats.Sitting = false;
                            EQStats.following = true;
                            
                            continue;
                        }
                        if (line.Contains("You are no longer auto-following"))
                        {
                            EQStats.Sitting = false;
                            EQStats.following = false;
                            Thread.Sleep(200);
                            continue;
                        }
                        if (line.Contains("To Arms!"))
                        {
                            RandomMove();
                            EQStats.Sitting = false;
                            EQStats.following = false;
                            Thread.Sleep(200);


                            continue;
                        }
                        if (line.Contains("Time For Buffs"))
                        {
                            RandomMove();

                            buffing = true;

                            continue;
                        }
                        if (line.Contains("No Buffs"))
                        {
                            RandomMove();

                            buffing = false;

                            continue;
                        }
                    }
                }
            });  // this processes the EQ log




            // Level <- 10 
            Thread ClericThread = new Thread(() =>
            {
                var ahk = AutoHotkeyEngine.Instance;
                ahk.ExecRaw("SetKeyDelay, 2");

                string directoryPath = "C:\\EQItems\\Cleric Buffs"; // Specify the directory path containing images
                Dictionary<string, Image<Bgr, byte>> images = new Dictionary<string, Image<Bgr, byte>>();

                // Check if the directory exists
                if (!Directory.Exists(directoryPath))
                {
                    Console.WriteLine("Directory does not exist.");
                }
                string[] imagePaths = Directory.GetFiles(directoryPath, "*.jpg"); // Change the file extension as needed

                foreach (string imagePath in imagePaths)  
                {
                    string imageName = Path.GetFileNameWithoutExtension(imagePath);
                    Image<Bgr, byte> image = new Image<Bgr, byte>(imagePath);
                    images.Add(imageName, image);
                }
                // above section loads the cleric images into memory in images dictionary


                while (!stopPrinting)
                {
                    List<int> PartyHP = new List<int>() { EQStats.My_HP, EQStats.Party1, EQStats.Party2, EQStats.Party3, EQStats.Party4, EQStats.Party5 };
                    if (EQStats.following)
                    {
                        EQStats.Sitting = false;
                        continue;
                    } // Do nothing, just follow
                    else if (EQStats.Casting == 1)  // Step 1, ensure your not casting
                    {

                        EQStats.Sitting = false;

                        if (EQStats.Target_HP == 100)
                        {
                            ahk.ExecRaw("SendEvent,x");
                            Thread.Sleep(20);
                            ahk.ExecRaw("SendEvent,x");
                        }
                        Thread.Sleep(200);
                        continue;
                    }
                    // healing block

                    for (int i = 0; i < 5; i++)  // scan each party members HP
                    {
                        if (PartyHP[i] < 70 && PartyHP[i] != 0 && PartyHP[i]! > 90 && PartyHP[i] != 100)  // top off lightly hurt people when you have mana
                        {
                            string Fcode = GetCurrentFValue(i);

                            EQStats.Casting = 1;
                            EQStats.Sitting = false;
                            ahk.ExecRaw("SendEvent," + Fcode);
                            Thread.Sleep(20);
                            ahk.ExecRaw("SendEvent,2");
                            break;

                        }
                        if (PartyHP[i] < 90 && PartyHP[i] > 0 && EQStats.My_MP > 90)
                        {
                            string Fcode = GetCurrentFValue(i);

                            EQStats.Casting = 1;
                            EQStats.Sitting = false;
                            ahk.ExecRaw("SendEvent,+" + Fcode);
                            Thread.Sleep(20);
                            ahk.ExecRaw("SendEvent,1");
                            break;

                        }

                    }

                    if (EQStats.Sitting == false && EQStats.Casting == -1)
                    {
                        Thread.Sleep(500);
                        EQStats.Sitting = true;
                        ahk.ExecRaw("SendEvent,-");
                    }
                    Thread.Sleep(500);
                    if (buffing == true)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            if (PartyHP[i] != -1 && PartyHP[i] != 0)
                            {
                                string Fcode = GetCurrentFValue(i);
                                ahk.ExecRaw("SendEvent,{Esc}");
                                Thread.Sleep(500);
                                ahk.ExecRaw("SendEvent," + Fcode);
                                Thread.Sleep(1000);

                                int x1 = BuffBox[0]; // x-coordinate of top-left corner
                                int y1 = BuffBox[1]; // y-coordinate of top-left corner
                                int x2 = BuffBox[2]; // x-coordinate of bottom-right corner
                                int y2 = BuffBox[3];

                                int width = Math.Abs(x2 - x1);
                                int height = Math.Abs(y2 - y1);

                                Bitmap screenCapture = CaptureScreenRegion(x1, y1, width, height);
                                Image<Bgr, byte> emguImage = screenCapture.ToImage<Bgr, byte>();

                                foreach (var kvp in images) // run through all the images
                                {
                                    string ImageName = kvp.Key;
                                    Image<Bgr, byte> image = kvp.Value;

                                    using (Image<Gray, float> result = emguImage.MatchTemplate(image, Emgu.CV.CvEnum.TemplateMatchingType.CcoeffNormed))
                                    {
                                        double[] minValues, maxValues;
                                        Point[] minLocations, maxLocations;
                                        result.MinMax(out minValues, out maxValues, out minLocations, out maxLocations);

                                        // Check if the similarity score is above a certain threshold
                                        double threshold = 0.8; // You can adjust this threshold as needed
                                        if (maxValues[0] > threshold)
                                        {
                                            Console.WriteLine("The smaller image is contained in the larger image.");
                                            continue;
                                        }
                                        else
                                        {
                                            Console.WriteLine("The smaller image is not contained in the larger image.");
                                            if (ImageName == "Armor")
                                            {
                                                EQStats.Sitting = false;
                                                Console.WriteLine("Needs Armor Spell");
                                                ahk.ExecRaw("SendEvent,6");
                                                Thread.Sleep(4500);
                                            }
                                            if (ImageName == "Symbol")
                                            {
                                                EQStats.Sitting = false;
                                                Console.WriteLine("Needs Symbol Spell");
                                                ahk.ExecRaw("SendEvent,5");
                                                Thread.Sleep(4500);
                                            }
                                            if (ImageName == "HPACBuff")
                                            {
                                                EQStats.Sitting = false;
                                                Console.WriteLine("Needs HPACBuff Spell");
                                                ahk.ExecRaw("SendEvent,4");
                                                Thread.Sleep(3500);
                                            }
                                        }

                                    }

                                }



                            }

                        }
                        buffing = false;

                    }
                }
                
            });

            Thread NecroThread = new Thread(() =>
            {
                while (!stopPrinting)
                {




                }

            });



            printThread.Start();
            ReadThread.Start();
            Read1.Start();
            Read2.Start();
            Read3.Start();
            ClericThread.Start();
            QueueThread.Start();








            // Wait for the user to press Enter to stop printing
            Console.ReadLine();

            stopPrinting = true; // Set the flag to stop the printing loop
            printThread.Join(); // Wait for the printThread to finish
            ReadThread.Join();
            Read1.Join();
            Read2.Join();
            Read3.Join();
            ClericThread.Join();
            QueueThread.Join();
            Console.WriteLine("Printing stopped.");
        }
    }


#if UNITY
    using UnityEngine;
#else
    using Mindmagma.Curses;
#endif

namespace ConsoleLogger
{
    public static class Logger
    {
        // public static Logger mainInstance = new Logger();
        public static int printImportance = 5;

//unity specefic code
#if UNITY

        public static void Write(object? value)
        {
            Debug.Log(value);
        }
        public void ClearCurrentConsoleLine()
        {
            throw new NotSupportedException("ClearCurrentConsoleLine is not supported when using unity");
        }
        public string ReadLine()
        {
            throw new NotSupportedException("ReadLine is not supported when using unity");
        }

//non unity specefic code
#else
        private static readonly object writingOutputLock = new();
        private static readonly object writinginputLock = new();
        private static readonly object accesConsoleInfo = new();

        private static IntPtr Screen = 0;
        private static IntPtr outputScreen = 0;
        private static IntPtr inputScreen = 0;

        static int maxRows = 0, maxCols = 0; 
        static Logger()
        {
            Console.WriteLine("starting ncurses");

            Screen = NCurses.InitScreen();
            NCurses.GetMaxYX(Screen, out maxRows, out maxCols);

            outputScreen = NCurses.SubWindow(Screen, maxRows - 1, maxCols, 0, 0);
            NCurses.ScrollOk(outputScreen, true);
            inputScreen = NCurses.SubWindow(Screen, 1, maxCols, maxRows - 1, 0);
            NCurses.ScrollOk(inputScreen, true);

            NCurses.

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(EmergencyCleanup);
        }
        
        public static void WriteLine(object? value)
        {
            WriteToConsole(value, true);
        }
        public static void WriteLine(object? value, bool isDebug, int importance = 3)
        {
#if DEBUG
            if(isDebug && importance <= printImportance) { WriteLine(value); }
#endif
        }
        public static void Write(object? value)
        {
            WriteToConsole(value, false);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value">the value to write</param>
        /// <param name="isDebug">wheather it should be considered a debug print</param>
        /// <param name="importance">will only print if lower or equal to Logger.printImportance</param>
        public static void Write(object? value, bool isDebug, int importance = 3)
        {
#if DEBUG
            if(isDebug && importance <= printImportance) { Write(value); }
#endif
        }
        static void WriteToConsole(object? value, bool useNewLine)
        {
            if(useNewLine)
            {
                SafeOuputWriteLine(value);
            }
            else
            {
                SafeOutputWrite(value);
            }
            return;

            /*int originalX = safeAccesCursorPosition().Left;
            int originalY = safeAccesCursorPosition().Top;

            ClearCurrentConsoleLine();
            SafeWrite(value);
            if(useNewLine) { SafeWrite('\n'); }

            redrawInput();

            safeWriteCursorPosition(originalX, Math.Clamp(originalY + 1, 0, safeAccessBufferSize().Width)); */
        }
        static void WriteRaw(string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                if(value[i] == '\n')
                {
                    Logger.Write(@"\\n");
                }
                else
                {
                    Logger.Write(value[i]);
                }
            }
        }
        private static void SafeOutputWrite(object? value)
        {
            lock (writingOutputLock) lock (accesConsoleInfo)
            {
                if(value == null) { return; }
                NCurses.TouchWindow(outputScreen);
                NCurses.WindowAddString(outputScreen, value.ToString());
            }
        }
        private static void SafeOuputWriteLine(object? value)
        {
            lock (writingOutputLock) lock (accesConsoleInfo)
            {
                SafeOutputWrite(value+"\n"); // console.writeline is !posssibly! not completly thread safe, but write is
            }
        }
        private static void SafeInputWrite(object? value)
        {
            lock (writinginputLock) lock (accesConsoleInfo)
            {
                if(value == null) { return; }
                NCurses.TouchWindow(inputScreen);
                NCurses.WindowAddString(inputScreen, value.ToString());
            }
        }
        private static void SafeInputWriteLine(object? value)
        {
            SafeInputWrite(value+"\n"); // console.writeline is !posssibly! not completly thread safe, but write is
        }
        static void redrawInput()
        {
            //int cursor = Console.CursorLeft;
            if(inputChars.Count > 0 || redoCommandIndex > 0)
            {
                // SafeWriteLine("writin inpu'");
                NCurses.ClearWindow(inputScreen);
                SafeInputWrite(
                    redoCommandIndex > 0 ?
                        commands[redoCommandIndex - 1] :
                        new string(inputChars.ToArray())
                );
            }
        } 
        private static readonly object readingLock = new();
        static List<string> commands = new List<string>();
        static int redoCommandIndex = 0; 
        static List<char> inputChars= new List<char>();
        public static string ReadLine()
        {
            
            lock (readingLock)
            {

                //return Console.ReadLine();
                
                string returnValue = "";
                while (true)
                {
                    int input = NCurses.GetChar();
                    (int left, int top) position = (0,0);
                    NCurses.GetYX(Screen, out position.top, out position.left);
                    Console.WriteLine("keycode: "+input);
                    if (inputScreen == 0) { Console.WriteLine("fuck"); }
                    switch (input)
                    {
                        case CursesKey.ENTER:
                            Console.WriteLine("enter pressed");
                            break;
                        case CursesKey.BACKSPACE:
                            //if(cursorIndex <= 0) {  continue; }

                            if(redoCommandIndex > 0) {inputChars = commands[redoCommandIndex - 1].ToCharArray().ToList(); redoCommandIndex = 0; }

                            redrawInput();
                            
                            continue;
                        case CursesKey.LEFT:
                            if(position.left > 0) { NCurses.WindowMove(inputScreen, position.left - 1, position.top); }
                            redrawInput();
                            continue;
                        case CursesKey.RIGHT:
                            if(position.left < inputChars.Count) { NCurses.WindowMove(inputScreen, position.left + 1, position.top); }
                            redrawInput();
                            continue;
                        case CursesKey.UP:
                            redoCommandIndex = Math.Clamp(redoCommandIndex++, 0, commands.Count);
                            redrawInput();
                            continue;
                        case CursesKey.DOWN:
                            redoCommandIndex = Math.Clamp(redoCommandIndex--, 0, commands.Count);
                            redrawInput();
                            continue;
                            
                        default:

                            if(redoCommandIndex > 0) {inputChars = commands[redoCommandIndex - 1].ToCharArray().ToList(); redoCommandIndex = 0; }

                            char inputChar = (char)input;

                            if(position.left >= inputChars.Count)
                            {
                                inputChars.Add(inputChar);
                                //SafeWrite(inputChar);
                                redrawInput();
                            }
                            else
                            {
                                //SafeWriteLine("test");
                                inputChars.Insert( Math.Clamp(position.left, 0, inputChars.Count), inputChar);
                                // SafeWrite(inputChar);
                                redrawInput();
                                // cursorIndex = safeAccesCursorPosition().Left;
                                // for(int i = cursorIndex; i < inputChars.Count; i++)
                                // {
                                //     SafeWrite(inputChars[i]);
                                // }
                                NCurses.WindowMove(outputScreen, position.left, position.top);
                            }
                            continue;
                    }
                    if (input == CursesKey.ENTER) {break;}
                    
                }
                if(redoCommandIndex > 0)
                {
                    returnValue = new string(commands[redoCommandIndex - 1]);
                    redoCommandIndex = 0;
                }
                else
                {
                    returnValue = new string(inputChars.ToArray()); // set return value
                }
                inputChars = new List<char>() {'h', 'i'}; //empty input list
                WriteLine(returnValue); //write command to console as history
                //ClearCurrentConsoleLine(); // clear line to be ready for next write or read
                
                commands.Reverse();
                commands.Add(returnValue);
                commands.Reverse();
                return returnValue;
            }
        }

        static void ShutdownConsole()
        {
            cleanup();
        }
        static void EmergencyCleanup(object sender, System.UnhandledExceptionEventArgs e)
        {
            try
            {
                cleanup();
            }
            catch
            {
                WriteLine("full loggerlib cleanup failed, doing partial cleanup");
                try
                {
                    NCurses.EndWin();
                }
                catch
                {
                    WriteLine("partial loggerlib cleanup failed");
                }
            }
        }
        static void cleanup()
        {
            NCurses.EndWin();
        }

        /* public static void ClearCurrentConsoleLine()
        {
            int currentLineCursor = safeAccesCursorPosition().Top;
            safeWriteCursorPosition(0, currentLineCursor);
            SafeWrite(new string(' ', safeAccesWindowSize().Width)); 
            safeWriteCursorPosition(0, currentLineCursor);
        }*/
        /* public static void ResetColor()
        {
            WriteLine("reseting console color");
            lock (writingLock) lock (accesConsoleInfo)
            {
                WriteLine("trying to reset console color", true, 4);
                Console.ResetColor();
                WriteLine("reseting console color sucess", true);
            }
        }  */
        /* public static (int Left, int Top) safeAccesCursorPosition()
        {
            lock (writingLock) lock (accesConsoleInfo)
            {
                return Console.GetCursorPosition();
            }
        }
        public static void safeWriteCursorPosition(int Left, int Top)
        {
            lock (writingLock) lock (accesConsoleInfo)
            {
                Console.SetCursorPosition(Left, Top);
            }
        }
        public static (int Height, int Width) safeAccesWindowSize()
        {
            lock (writingLock) lock (accesConsoleInfo)
            {
                return (Console.WindowHeight, Console.WindowWidth);
            }
        }
        public static (int Height, int Width) safeAccessBufferSize()
        {
            lock (writingLock) lock (accesConsoleInfo)
            {
                return (Console.BufferHeight, Console.BufferWidth);
            }
        } */
#endif
    }
}
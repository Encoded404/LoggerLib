#if UNITY
    using UnityEngine;
#else
    using ConsoleLogger;
#endif

namespace ConsoleLogger
{
    public class Logger
    {
        public static int printImportance = 5;

//unity specefic code
#if UNITY

        public static void Write(object? value)
        {
            Debug.Log(value);
        }
        public static void WriteLine(object? value)
        {
            write(value);
        }
        public static void ClearCurrentConsoleLine()
        {
            throw new NotSupportedException("ClearCurrentConsoleLine is not supported when using unity");
        }
        public static string ReadLine()
        {
            throw new NotSupportedException("ReadLine is not supported when using unity");
        }

//non unity specefic code
#else

        static List<char> inputChars= new List<char>();

        static List<string> commands = new List<string>();
        static int redoCommandIndex = 0;
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
        private static readonly object accesConsoleInfo = new();
        public static (int Left, int Top) safeAccesCursorPosition()
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
        public static void safeWriteCursorVisibility(bool visible)
        {
            lock (writingLock) lock (accesConsoleInfo)
            {
                Console.CursorVisible = visible;
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
        }
        private static readonly object writingLock = new();
        static void WriteToConsole(object? value, bool useNewLine)
        {
            /*if(useNewLine)
            {
                SafeWriteLine(value);
            }
            else
            {
                SafeWrite(value);
            }
            return;*/

            int originalX = safeAccesCursorPosition().Left;
            int originalY = safeAccesCursorPosition().Top;

            ClearCurrentConsoleLine();
            SafeWrite(value);
            if(useNewLine) { SafeWrite('\n'); }

            redrawInput();

            safeWriteCursorPosition(originalX, Math.Clamp(originalY + 1, 0, safeAccessBufferSize().Height));
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
        private static void SafeWrite(object? value)
        {
            lock (writingLock) lock (accesConsoleInfo)
            {
                Console.Write(value);
            }
        }
        private static void SafeWriteLine(object? value)
        {
            lock (writingLock) lock (accesConsoleInfo)
            {
                SafeWrite(value+"\n"); // console.writeline is !posssibly! not completly thread safe, but write is
            }
        }
        public static void ClearCurrentConsoleLine()
        {
            int currentLineCursor = safeAccesCursorPosition().Top;
            SafeWrite("\r");
            SafeWrite(new string(' ', safeAccesWindowSize().Width - 1)); 
            SafeWrite("\r");
        }
        static void redrawInput()
        {
            redoCommandIndex = Math.Clamp(redoCommandIndex, 0, commands.Count);
            //int cursor = Console.CursorLeft;

            ClearCurrentConsoleLine();

            if(inputChars.Count > 0 || redoCommandIndex > 0)
            {
                SafeWrite(
                    redoCommandIndex > 0 ?
                        commands[redoCommandIndex - 1] :
                        new string(inputChars.ToArray())
                );
            }
        }
        private static readonly object readingLock = new();
        public static string ReadLine()
        {
            lock (readingLock)
            {

                //return Console.ReadLine();
                
                string returnValue = "";
                while (true)
                {
                    ConsoleKeyInfo input = Console.ReadKey(true);
                    int cursorIndex = safeAccesCursorPosition().Left;
                    switch (input.Key)
                    {
                        case ConsoleKey.Enter:
                            break;
                        case ConsoleKey.Backspace:
                            if(cursorIndex <= 0) {  continue; }

                            if(redoCommandIndex > 0) {inputChars = commands[redoCommandIndex - 1].ToCharArray().ToList(); redoCommandIndex = 0; }

                            //WriteLine("delete pressed");
                            if(inputChars.Count > 0) { inputChars.RemoveAt(Math.Clamp(cursorIndex - 1, 0, inputChars.Count-1)); }
                            
                            // ClearCurrentConsoleLine();
                            // //SafeWrite("content is: ");
                            // foreach(char ch in inputChars)
                            // {
                            //     SafeWrite(ch);
                            // }
                            redrawInput();

                            safeWriteCursorPosition(Math.Clamp(cursorIndex - 1, 0, safeAccessBufferSize().Width), safeAccesCursorPosition().Top);

                            continue;
                        case ConsoleKey.Delete:
                            if(cursorIndex == inputChars.Count) {  continue; }

                            if(redoCommandIndex > 0) {inputChars = commands[redoCommandIndex - 1].ToCharArray().ToList(); redoCommandIndex = 0; }

                            //WriteLine("delete pressed");
                            if(inputChars.Count > 0) { inputChars.RemoveAt(Math.Clamp(cursorIndex, 0, inputChars.Count-1)); }

                            redrawInput();

                            safeWriteCursorPosition(Math.Clamp(cursorIndex, 0, safeAccessBufferSize().Width), safeAccesCursorPosition().Top);
                            continue;
                        case ConsoleKey.LeftArrow:
                            if(safeAccesCursorPosition().Left > 0) { safeWriteCursorPosition(safeAccesCursorPosition().Left - 1, safeAccesCursorPosition().Top); }
                            continue;
                        case ConsoleKey.RightArrow:
                            if(safeAccesCursorPosition().Left < inputChars.Count) { safeWriteCursorPosition(safeAccesCursorPosition().Left + 1, safeAccesCursorPosition().Top); }
                            continue;
                        case ConsoleKey.UpArrow:
                            redoCommandIndex++;
                            redrawInput();
                            continue;
                        case ConsoleKey.DownArrow:
                            redoCommandIndex--;
                            redrawInput();
                            continue;
                            
                        default:

                            if(redoCommandIndex > 0) {inputChars = commands[redoCommandIndex - 1].ToCharArray().ToList(); redoCommandIndex = 0; }

                            char inputChar = input.KeyChar;
                            if(safeAccesCursorPosition().Left >= inputChars.Count)
                            {
                                inputChars.Add(inputChar);
                                //SafeWrite(inputChar);
                                redrawInput();
                            }
                            else
                            {
                                //SafeWriteLine("test");
                                inputChars.Insert( Math.Clamp(safeAccesCursorPosition().Left, 0, inputChars.Count), inputChar);
                                // SafeWrite(inputChar);
                                redrawInput();
                                // cursorIndex = safeAccesCursorPosition().Left;
                                // for(int i = cursorIndex; i < inputChars.Count; i++)
                                // {
                                //     SafeWrite(inputChars[i]);
                                // }
                                safeWriteCursorPosition(cursorIndex, safeAccesCursorPosition().Top);
                            }
                            continue;
                    }
                    if (input.Key == ConsoleKey.Enter) {break;}
                    
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
                inputChars = new List<char>(); //empty input list
                WriteLine(returnValue); //write command to console as history
                ClearCurrentConsoleLine(); // clear line to be ready for next write or read
                
                commands.Insert(0, returnValue);
                return returnValue;
            }
        }
        public static void ResetColor()
        {
            WriteLine("reseting console color");
            lock (writingLock) lock (accesConsoleInfo)
            {
                WriteLine("trying to reset console color", true, 4);
                Console.ResetColor();
                WriteLine("reseting console color sucess", true);
            }
        }
#endif
    }
}
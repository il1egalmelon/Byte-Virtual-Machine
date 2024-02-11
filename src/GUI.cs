/*
    Copyright 2024 il1egalmelon
*/

using System;
using System.Collections.Generic;

internal struct Info {
    public static string[] txt     = new string[] {
            "File                 : ",
            "Memory stack (bytes) : ",
            "Memory heap  (bytes) : ",
            "Execution mode       : "
    };

    public static int      cursorY = 0;

    public static string   file    = "";
    public static long     mStack  = 0;
    public static long     mHeap   = 0;
    public static string   mode    = "";
}

class GUI {
    public static List<string> GuiMain() {
        Console.CursorVisible = false;
        while (true) {
            PrintOut();
            var keyPress = Console.ReadKey(true);
            
            switch (keyPress.Key) {
                case ConsoleKey.DownArrow:
                    if (Info.cursorY + 1 < Info.txt.Length)
                        Info.cursorY++;

                    break;

                case ConsoleKey.UpArrow:
                    if (Info.cursorY > 0)
                        Info.cursorY--;

                    break;

                case ConsoleKey.Enter:
                    FieldSetter();

                    break;

                case ConsoleKey.Escape:
                    Console.SetCursorPosition(0, Info.txt.Length + 2);
                    Console.Write("Are you sure? (Y / N): ");
                    Console.CursorVisible = true;
                    if (Console.ReadLine() == "Y")
                        goto outofloop;

                    Console.CursorVisible = false;

                    break;
            }
        }

    outofloop:
        Console.CursorVisible = true;
        Console.Clear();
        List<string> ret = new List<string>();
        ret.Add(Info.file);
        ret.Add(Info.mStack.ToString());
        ret.Add(Info.mHeap.ToString());
        ret.Add(Info.mode);
        return ret;
    }

    static void PrintOut() {
        Console.Clear();
        for (int i = 0; i < Info.txt.Length; i++) {
            if (i == Info.cursorY) {
                Console.BackgroundColor = ConsoleColor.White;
                Console.ForegroundColor = ConsoleColor.DarkGray;
            }

            int restFiller = 0;

            restFiller += ConsoleWrite(Info.txt[i]);

            switch (i) {
                case 0:
                    restFiller += ConsoleWrite(Info.file);
                    break;
                case 1:
                    if (Info.mStack != 0)
                        restFiller += ConsoleWrite(Info.mStack.ToString());
                    break;
                case 2:
                    if (Info.mHeap != 0)
                        restFiller += ConsoleWrite(Info.mHeap.ToString());
                    break;
                case 3:
                    restFiller += ConsoleWrite(Info.mode);
                    break;
            }

            for (int l = 0; l < Console.WindowWidth - restFiller; l++) {
                Console.Write(' ');
            }

            Console.ResetColor();
        }
    }

    static int ConsoleWrite(string text) {
        Console.Write(text);
        return text.Length;
    }

    static int ConsoleWriteLine(string text) {
        Console.WriteLine(text);
        return text.Length;
    }

    static void FieldSetter() {
        switch (Info.cursorY) {
            case 0:
                Info.file = "";
                break;
            case 1:
                Info.mStack = 0;
                break;
            case 2:
                Info.mHeap = 0;
                break;
            case 3:
                Info.mode = "";
                break;
        }

        PrintOut();

        Console.SetCursorPosition(Info.txt[Info.cursorY].Length, Info.cursorY);
        Console.CursorVisible = true;
        Console.BackgroundColor = ConsoleColor.White;
        Console.ForegroundColor = ConsoleColor.DarkGray;

        string input = "" + Console.ReadLine();

        try {
            switch (Info.cursorY) {
                case 0:
                    Info.file = input;
                    break;
                case 1:
                    Info.mStack = Convert.ToInt64(input);
                    break;
                case 2:
                    Info.mHeap = Convert.ToInt64(input);
                    break;
                case 3:
                    Info.mode = input;
                    break;
            }
        } catch {}

        if (Info.cursorY + 1 < Info.txt.Length)
            Info.cursorY++;

        Console.ResetColor();
        Console.CursorVisible = false;
    }
}

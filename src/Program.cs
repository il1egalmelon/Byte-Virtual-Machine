/*
    Copyright 2024 il1egalmelon
*/

using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

#pragma warning disable CS8618
#pragma warning disable CS0649
#pragma warning disable CS8604

/*
 *
 * EXIT STATUSES:
 *  0 pass
 *  1 invokation fail
 *  2 runtime fail
 *
 */

struct ObjectMapTemplate {
    /*
     *
     * EXAMPLE USAGE FOR ObjectMapTemplate STRUCT:
     *  objectMap.Add(
     *  new ObjectMapTemplate {
     *      varName = "x",
     *      scope = 0,
     *      locationIndexZero = 0;
     *      typeSize = 4,
     *      size = 4
     *  });
     *
     */

    public string varName;
    public int    scope;
    public long   locationIndexZero;
    public int    typeSize;
    public long   size;
}

class Runtime {
    public string[] code;
    public byte[]   stack;
    public byte[]   heap;

    public List<ObjectMapTemplate> objectMap = new List<ObjectMapTemplate>();
    public List<(string, int)> cachedLabels = new List<(string, int)>(); 
    public System.Diagnostics.Stopwatch GCtimeMS = new System.Diagnostics.Stopwatch();

    public long     linePointer = 0;  //instruction/line pointer
    public long     SRC         = 0;  //source register
    public long     SND         = 0;  //source & dest register
    public long     STKPNT      = 0;  //stack pointer
    public long     HEPPNT      = 0;  //heap pointer

    public bool     flag_EQ     = false;  //flag equal
    public bool     flag_NQ     = false;  //flag not equal
    public bool     flag_GT     = false;  //flag greater than
    public bool     flag_LT     = false;  //flag less than
    public string   loadedLabel = "";

    //everything is 0 indexed
    public int      scope       = 0;
    public int      layer       = 0;
    public long     entry       = -1;

    public void RuntimeEntryPointNormal() {
        if (code[0] != "Start") {
            Console.WriteLine("[EE] : No \"Start\" keyword found at first line!");
            throw new Exception("RUNTIME ERROR");
        }

        //cleanup tabs
        for (int i = 0; i < code.Length; i++) {
            code[i] = Macros.RemoveInitialSpaces(code[i]);
            code[i] = Macros.RemoveTrailingSpaces(code[i]);
        }

        entry = Macros.FindSymbol("Main()");
        if (entry == -1) {
            Console.WriteLine("[EE] : No \"Main()\" entry point!");
            throw new Exception("RUNTIME ERROR");
        }

        linePointer = entry;

        while (true) {
            Inline.RunLine(code[linePointer]);

            if (linePointer + 1 < code.Length) {
                linePointer++;
            } else {
                Console.WriteLine("[EE] : No \"End\" keyword found at the end!");
                throw new Exception("RUNTIME ERROR");
            }

            //Debug.DumpAll();
            //Console.WriteLine(code[linePointer]);
            //Debug.PrintOutStackFrame();
        }
    }
}

class MainFunction {
    public static Runtime instancedRuntime;
    public static string  FILEPATH = "";

    public static void Main(string[] arguments) {
        //arg0: Filepath
        //arg1: Memory Stack
        //arg2: Memory Heap

        List<string> args;

        if (arguments.Length != 4) {
            args = new List<string>();
            args = GUI.GuiMain();
        } else {
            args = new List<string>(arguments);
        }

        (string filepath, long mStack, long mHeap, string mode) = ParseStartupInput(args.ToArray());
        FILEPATH = filepath;

        //instantiate an instance
        instancedRuntime = new Runtime();
        try {
            instancedRuntime.code = File.ReadAllLines(filepath);
        } catch {
            Console.WriteLine("[EE] : Failed to read file!");
            Environment.Exit(1);
        }

        try {
            instancedRuntime.stack = new byte[mStack];
            instancedRuntime.heap = new byte[mHeap];
        } catch {
            Console.WriteLine("[EE] : Failed to allocate memory!");
            Environment.Exit(1);
        }

        if (mode == "optimized") {
            //Macros.ReplaceKeywords();

            mode = "normal";
        }

        //start the instance
        if (mode == "normal") {
            Stopwatch watch = new Stopwatch();

            try {
                watch.Start();
                instancedRuntime.RuntimeEntryPointNormal();
            } catch (Exception ex) {
                if (ex.Message == "DONE") {
                    goto DONE;
                }

                if (instancedRuntime.STKPNT > instancedRuntime.stack.Length - 1) {
                    Console.WriteLine("[EE] : Out of stack memory!");
                }

                Console.WriteLine(ex);
                for (int i = 0; i < Console.WindowWidth; i++) {
                    Console.Write("-");
                }
                Console.WriteLine();

                Console.WriteLine("OBJECT DUMP: ");
                Debug.DumpObjectVariables(instancedRuntime);
            }

        DONE:
            watch.Stop();
            Console.WriteLine("\n\nExecution time: " + watch.ElapsedMilliseconds + "ms");
            Console.WriteLine(    "GC time:        " + instancedRuntime.GCtimeMS.ElapsedMilliseconds + "ms");

            if (arguments.Length != 4) {
                Console.ReadKey();
            }
        }
        else {
            Console.WriteLine("[EE] : Wrong execution mode!");
            Environment.Exit(1);
        }
    }

    private static (string filepath, long mS, long mH, string mode) ParseStartupInput(string[] args) {
        try {
            return (args[0], Convert.ToInt64(args[1]), Convert.ToInt64(args[2]), args[3]);
        } catch {
            Console.WriteLine("[EE] : Invalid startup arguments!");
            Environment.Exit(1);
            return ("", 0L, 0L, "");
        }
    }
}

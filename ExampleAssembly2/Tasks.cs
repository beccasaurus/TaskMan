using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaskMan;

namespace TaskMan.Specs.ExampleAssembly2 {
    public class Tasks {

        static public string output = "";

        [Task("before1", Description = "This is Before #1")]
        public static void Before1() {
            output = "BEFORE1";
        }

        [Task("before2", Description = "Before 2!")]
        public static void Before2() {
            output += " before2";
        }

        [Task("callback:example", Before = "before1 before2", After = "after1 after2 after3", Description = "This calls a BUNCH of stuff")]
        public static string CallbackExample() {
            output += " <THE CODE>";
            return output;
        }

        [Task("after1", Description = "This is after #1")]
        public static void After1() {
            output += " after1";
        }

        [Task("after2", Description = "This is AFTER #2")]
        public static void After2() {
            output += " AFTER2";
        }

        [Task("after3", Description = "This is After #3")]
        public static void After3() {
            output += " AFTER3!";
        }

        [Task("get:output")]
        public static string GetOutput() {
            return output;
        }

        [Task("print:output")]
        public static void PrintOutput() {
            Console.WriteLine(output);
        }
    }
}

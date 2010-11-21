using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TaskMan.Specs.ExampleAssembly1 {
    public class Tasks {

        static public int Number = 0;

        [TaskMan.Task("foobar", Description = "Returns 'Foo Bar'")]
        public static string ReturnFooBar() {
            return "Foo Bar";
        }

        [TaskMan.Task("increment:number")]
        public static void IncrementTasksNumber() {
            Number++;
        }

    }
}

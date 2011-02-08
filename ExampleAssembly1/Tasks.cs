using System;
using TaskMan;

namespace TaskMan.Specs.ExampleAssembly1 {
    public class Tasks {

        static public int Number = 0;

        [Task("foobar", "Returns 'Foo Bar'")]
        public static string ReturnFooBar() {
            return "Foo Bar";
        }

        [Task]
        public static void IncrementNumber() {
            Number++;
        }

    }
}

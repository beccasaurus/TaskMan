using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TaskMan {
    public static class ProcessRunner {

        // Usage:
        //   "ls -lrt".Exec();
        public static string Exec(this string str) {
            return ProcessRunner.RunCommand(str);
        }

        public static string RunCommand(string command) {
            command   = command.Trim();
            int space = command.IndexOf(' ');
            if (space < 0)
                return RunCommandWithArguments(command, null);
            else
                return RunCommandWithArguments(command.Substring(0, space), command.Substring(space + 1));
        }

        public static string RunCommandWithArguments(string command, string arguments) {
            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = command;
            if (arguments != null)
                process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute        = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow         = true;
            process.Start();
            string stdout = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return stdout;
        }

    }
}

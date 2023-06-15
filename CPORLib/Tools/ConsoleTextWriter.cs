using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace CPORLib.Tools
{
    public class ConsoleTextWriter : TextWriter
    {
        public TextWriter ConsoleOut;
        public bool Active;


        

        private static ConsoleTextWriter Instance = new ConsoleTextWriter();
        public static ConsoleTextWriter Get()
        {
            return Instance;
        }

        private ConsoleTextWriter()
        {
            ConsoleOut = Console.Out;
            Active = true;
        }

        public override void Write(string s)
        {
            if(Active)
            {
                ConsoleOut.Write(s);
            }
        }
        public override void WriteLine(string s)
        {
            if( Active)
            {
                ConsoleOut.WriteLine(s);
            }
        }
        public override Encoding Encoding => ConsoleOut.Encoding;
    }
}

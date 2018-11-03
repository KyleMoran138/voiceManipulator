using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace methods {
    class Program {
        static void Main(string[] args) {
            var method = typeof(Program).GetMethod("method");
            method.Invoke(null, null);
            Console.ReadKey();
        }


        public static void method() {
            Console.WriteLine("SGDAG");
        }
    }
}

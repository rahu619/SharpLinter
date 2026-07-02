using System;

namespace SharpLinter.ClientTest
{
    // Violation: Class name should use PascalCase (SL1005)
    class my_bad_class
    {
        // Violation: Public field (SL1003)
        public string badField = "No encapsulation";

        public static void Main(string[] args)
        {
            Console.WriteLine("Starting Verification Client...");

            // Violation: Missing braces on control flow (SL1001)
            if (args.Length == 0)
                Console.WriteLine("No arguments provided.");
            else
                Console.WriteLine("Arguments provided.");

            // Violation: String concatenation in loop (SL1012)
            string result = "";
            for (int i = 0; i < 5; i++)
            {
                result += i.ToString();
            }
            Console.WriteLine("Loop result: " + result);
        }
    }
}

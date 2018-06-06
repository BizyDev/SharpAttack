using System;
using System.Threading;

namespace SkypeAPI
{
    public class Program
    {

        static void Main(string[] args)
        {

            CallManagement call = new CallManagement();
            new Thread(() => { call.Init(); }).Start();

            Console.ReadKey();
            //readkey not sufficient. closes on skype close -_-
            while (true)
            {
                Thread.Sleep(500);
            }
        }


    }
}

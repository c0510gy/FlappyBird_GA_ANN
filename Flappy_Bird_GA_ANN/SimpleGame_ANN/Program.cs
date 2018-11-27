using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleGame_ANN
{
    class Program
    {
        [DllImport("kernel32.dll", EntryPoint = "GetConsoleWindow", SetLastError = true)]
        private static extern IntPtr GetConsoleHandle();
        static IntPtr handler = GetConsoleHandle();

        static SimpleGame sg;
        static void Main(string[] args)
        {
            int gen = 0; //세대
            int pre_Best_Score = 0;
            GA_ANN gn = new GA_ANN();
            Console.WriteLine("Press ESC to stop");
            while (true)
            {
                gen++;

                gn.Mating();

                Console.Write("\r" + gen + "세대 | 현재까지 최고 점수 : " + pre_Best_Score + "                         ");
                
                if (gn.Best_Score > pre_Best_Score)
                {
                    Console.Write("\r" + gen + "세대 | 현재까지 최고 점수 : " + gn.Best_Score + "                         ");
                    
                    int n_score = gn.DNASimul(gn.Best_index, handler);
                    System.IO.File.WriteAllText(@"datas\" + gen + @"\info_score_" + gn.Best_index + ".txt", n_score + "");
                    if (n_score <= pre_Best_Score)
                    {
                        System.IO.Directory.Delete(@"datas\" + gen + "_best", true);
                        Console.Write("\r" + gen + "세대 | (재수정)현재까지 최고 점수 : " + pre_Best_Score + "                         ");
                        Thread.Sleep(100);
                    }
                    else
                    {
                        pre_Best_Score = n_score;
                    }
                    
                }

                if (Console.KeyAvailable)
                {
                    var tmp = Console.ReadKey(true).Key;
                    if (tmp == ConsoleKey.Escape)
                    {
                        break;
                    }
                }
            }


            /*
            sg = new SimpleGame(800, 400);
            Console.WriteLine("Press ESC to stop");
            while (true)
            {
                sg.NextFrame();
                sg.DrawFrame();

                Console.Write("\r" + sg.nearXDis + ", " + sg.nearYDis);

                using (var graphics = Graphics.FromHwnd(handler))
                using (var image = (Image)sg.visual)
                    graphics.DrawImage(image, 50, 50, image.Width, image.Height);

                if (sg.GameOver)
                {
                    break;
                }

                Thread.Sleep(33);
                if (Console.KeyAvailable)
                {
                    var tmp = Console.ReadKey(true).Key;
                    if (tmp == ConsoleKey.Escape)
                    {
                        break;
                    }else if (tmp == ConsoleKey.Spacebar)
                    {
                        sg.rnr.Jump();
                    }
                }
            }
            */
        }
    }
}

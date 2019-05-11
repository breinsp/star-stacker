using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ImageStacking.Stacking
{
    public class StackingController
    {
        public static StackingController instance;

        public List<Image> queue;
        public List<Image> finished;
        public List<BaseThread> threads;

        public StackingController()
        {
            if (instance != null) throw new Exception("instance is already set!");
            instance = this;
        }

        public void Run()
        {
            queue = new List<Image>();
            finished = new List<Image>();
            StartThreads();
            ReadImages();
            WaitForCompletion();
        }

        public void StartThreads()
        {
            threads = new List<BaseThread>();
            for (int i = 0; i < 4; i++)
            {
                threads.Add(new WorkerThread() { id = i });
            }
            foreach (var t in threads)
            {
                t.Start();
            }
            Console.WriteLine(threads.Count + " threads started.");
        }

        public void StopThreads()
        {
            foreach (var t in threads)
            {
                t.Stop();
            }
            Console.WriteLine("Threads terminated");
        }

        public void ReadImages()
        {
            var dir = new List<string>(Directory.EnumerateFiles("./Testdata/"));
            int id = 0;
            int index = dir.Count / 2;

            string first = dir[index];
            dir.RemoveAt(index);

            Image firstImage = ImageLoader.LoadImage(first);
            firstImage.Id = id++;
            ImageProcessor.FindCornerPoints(firstImage);
            lock (queue)
            {
                queue.Add(firstImage);
            }

            foreach (var f in dir)
            {
                Image image = ImageLoader.LoadImage(f);
                image.Id = id++;
                lock (queue)
                {
                    queue.Add(image);
                }
            }
        }

        public void WaitForCompletion()
        {
            while (!IsFinished())
            {
                Thread.Sleep(100);
            }
            StopThreads();

            Image first = queue[0];
            List<Image> total = new List<Image>();
            total.Add(first);
            total.AddRange(finished);

            Image result = ImageProcessor.StackResult(total);
            ImageLoader.WriteImage("result.bmp", result);
        }



        public bool IsFinished()
        {
            if (queue.Count == 1)
            {
                foreach (var t in threads)
                {
                    if (t.working) return false;
                }
                return true;
            }
            return false;
        }

    }
}

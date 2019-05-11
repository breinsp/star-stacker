using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageStacking.Stacking
{
    public class WorkerThread : BaseThread
    {

        public override void Action()
        {
            StackingController instance = StackingController.instance;
            if (instance.queue.Count > 1)
            {
                working = true;
                Image image1 = null;
                Image image2 = null;
                lock (instance.queue)
                {
                    if (instance.queue.Count > 1)
                    {
                        image1 = instance.queue[0];
                        image2 = instance.queue[1];
                        instance.queue.RemoveAt(1);
                    }
                }
                if (image1 != null && image2 != null)
                {
                    bool valid = false;
                    Image result = ImageProcessor.TransformImage(image1, image2, out valid);
                    lock (instance.finished)
                    {
                        instance.finished.Add(result);
                    }
                    if (valid)
                        Debug(image2.Filename + " transformed.");
                }
                else
                {
                    working = false;
                }
            }
            else
            {
                working = false;
            }
            System.Threading.Thread.Sleep(50);
        }
    }
}

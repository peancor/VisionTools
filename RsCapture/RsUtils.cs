using Intel.RealSense;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RsCapture
{
    public class RsUtils
    {
        public static void FillDepth(Frame df, ushort[] depthBuffer, int width, int height)
        {
            unsafe
            {
                fixed (ushort* p = depthBuffer)
                {
                    Buffer.MemoryCopy(df.Data.ToPointer(), p, 2 * width * height, 2 * width * height);
                }
            }
        }
    }
}

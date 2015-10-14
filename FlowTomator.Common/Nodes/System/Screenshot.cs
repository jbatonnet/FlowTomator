using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FlowTomator.Common
{
    [Node("Screenshot", "System", "Takes a screenshot of the specified screen")]
    public class Screenshot : Task
    {
        public override IEnumerable<Variable> Outputs
        {
            get
            {
                yield return bitmap;
            }
        }

        private Variable<Bitmap> bitmap = new Variable<Bitmap>("Bitmap", null, "The neewly captured screenshot");

        public override NodeResult Run()
        {
            // Setup the bitmap
            Bitmap bitmap = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height, PixelFormat.Format32bppArgb);
            Graphics graphcis = Graphics.FromImage(bitmap);

            // Take the screenshot
            graphcis.CopyFromScreen(Screen.PrimaryScreen.Bounds.X, Screen.PrimaryScreen.Bounds.Y, 0, 0, Screen.PrimaryScreen.Bounds.Size, CopyPixelOperation.SourceCopy);

            // Output the bitmap
            this.bitmap.Value = bitmap;

            return NodeResult.Success;
        }
    }
}
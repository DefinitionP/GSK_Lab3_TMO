using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMO
{
    /* класс для хранения информации о закрашивании сегментов строк изображения */
    public class Raster
    {
        public RasterString[] strings;
        public int height, width;

        public Raster() { }
        public Raster(int height, int width)
        {
            strings = new RasterString[height];
            this.height = height;
            this.width = width;
            for (int i = 0; i < height; i++)
            {
                strings[i] = new RasterString(this);
            }
        }
        public RasterString this[int index]
        {
            get { return strings[index]; }
        }
        public void FillRange(int from, int to)
        {
            for (int i = from; i < to; i++)
            {
                strings[i].Fill();
            }
        }
        public void Draw(DrawingMethod method, Color background)
        {
            int Y = height - 1;
            if (strings.Count() == 0) return;

            foreach (var str in strings)
            {
                // закрашивание изображения (снизу вверх)
                for (int i = str.Xl.Count - 1; i >= 0; i--)
                {
                    for (int j = str.Xl[i]; j <= str.Xr[i]; j++) method(j, Y, background);
                }
                Y--;
            }
        }
        public void Clear()
        {
            foreach (var str in strings)
            {
                str.Xr.Clear(); str.Xl.Clear();
            }
        }
    }
    /* класс для хранения информации о координатах закрашиваемых 
     сегментов отдельной строки */
    public class RasterString
    {
        // список начальных координат закрашиваемых сегментов
        public List<int> Xl;
        // список конечных координат закрашиваемых сегментов
        public List<int> Xr;
        Raster parent;

        public RasterString(Raster parent)
        {
            Xl = new List<int>();
            Xr = new List<int>();
            this.parent = parent;
        }
        public void Fill()
        {
            Xl.Add(0);
            Xr.Add(parent.width - 1);
        }
        public void Sort()
        {
            Xl = Xl.OrderBy(x => x).ToList();
            Xr = Xr.OrderBy(x => x).ToList();  
        }
    }

    public delegate void DrawingMethod(int x, int y, Color Color);
}

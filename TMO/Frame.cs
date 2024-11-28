using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;

namespace TMO
{
    public enum TMOtype
    {
        none = 0, conjunction = 1, intersection = 2, symm_diff = 3, diff_AB = 4, diff_BA = 5
    }

    // класс для работы с кадрами
    public class Frame
    {
        public static int default_canvas_width = 500;
        public static int default_canvas_height = 200;

        // создание потока для сохранения Bitmap в ОП
        public static MemoryStream ms = new MemoryStream();
        private static Bitmap blank_canvas;
        
        // список многоугольников
        public List<Polygon> polygons;
        // свойство, возвращающее ссылку на последний многоугольник в списке
        public Polygon active_polygon
        {
            get
            {
                if (polygons.Count == 0) return null; /*curves.Add(new Curve());*/
                return polygons.Last();
            }
        }
        public Frame()
        {
            blank_canvas = new Bitmap(default_canvas_width, default_canvas_height);
            White(ref blank_canvas);
            polygons = new List<Polygon>();
        }
        public Frame(Frame src)
        {
            polygons = new List<Polygon>();
            foreach (var src_polygon in src.polygons) polygons.Add(src_polygon.Copy());
        }
        public BitmapImage Draw(bool conditions, bool fill, TMOtype tmo)
        {
            Bitmap current = new Bitmap(blank_canvas);
            // обработка данных многоугольников и закрашивание (если не выбрано ТМО)
            for (int j = 0; j < polygons.Count(); j++)
            {
                Polygon polygon = polygons[j];
                polygon.Process(conditions);
                if (fill && polygon.raster.strings != null)
                {
                    byte k = 0b01111111;
                    Color background = Color.FromArgb(255, (byte)(polygon.color.R | k), (byte)(polygon.color.G | k), (byte)(polygon.color.B | k));
                    polygon.raster.Draw((int x, int y, Color color) => current.SetPixel(x, y, color), background);
                    //curve.raster.Clear();
                }
            }
            // закрашивание результата ТМО
            if (tmo != TMOtype.none && polygons.Count == 2)
            {
                Polygon A = polygons[0];
                Polygon B = polygons[1];
                if (A.points.Count > 2 || B.points.Count > 2)
                {
                    // расчёт цвета области результата ТМО
                    Color tmo_result_color = Color.FromArgb(255, 
                        (A.color.R + B.color.R) / 2,
                        (A.color.G + B.color.G) / 2,
                        (A.color.B + B.color.B) / 2);
                    Raster r = TMO(tmo, ref current);
                    r.Draw((int x, int y, Color color) => current.SetPixel(x, y, color), tmo_result_color);
                }
            }
            // отрисовка вершин и границ
            for (int j = 0; j < polygons.Count(); j++)
            {
                Polygon polygon = polygons[j];
                for (int i = 0; i < polygon.pixel_coords.Count; i++)
                {
                    var dot = polygon.pixel_coords[i];
                    // обработка случая. когда точка выходит за границы изображения
                    if (dot.X > current.Width - 1 || dot.X < 0) continue;
                    if (dot.Y > current.Height - 1 || dot.Y < 0) continue;
                    current.SetPixel(dot.X, dot.Y, polygon.color);
                }
                polygon.pixel_coords.Clear();
            }
            current.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            bitmapImage.StreamSource = ms;
            bitmapImage.EndInit();
            return bitmapImage;
        }

        Raster TMO(TMOtype tmo, ref Bitmap b)
        {
            Raster tmo_result = new Raster(default_canvas_height, default_canvas_width);
            Polygon A = polygons[0];
            Polygon B = polygons[1];
            if (A.points.Count < 3 || B.points.Count < 3) return tmo_result;
            int[] SetQ = new int[2] {0, 0};
            switch (tmo) {
                case TMOtype.conjunction: { SetQ[0] = 1; SetQ[1] = 3; break; }
                case TMOtype.intersection: { SetQ [0] = 3; SetQ[1] = 3; break; }
                case TMOtype.symm_diff: { SetQ[0] = 1; SetQ[1] = 2; break; }
                case TMOtype.diff_AB: { SetQ[0] = 2; SetQ[1] = 2; break; }
                case TMOtype.diff_BA: { SetQ[0] = 1; SetQ[1] = 1; break; }
            }
            int height = A.raster.height;
            int[] Mx = new int[height];
            int[] MdQ = new int[height];

            int stripe_width = 2, stripe_counter = 1;
            bool stripe = false;

            for (int str = 0; str < height; str++)
            {
                stripe_counter++;
                if (stripe_counter > stripe_width) { stripe = !stripe; stripe_counter = 0; }
                if (stripe) continue;
                List<(int, int)> pairs = new List<(int, int)>();
                RasterString rstrA = A.raster[str];
                RasterString rstrB = B.raster[str];

                int n = rstrA.Xr.Count + rstrA.Xl.Count;
                int nM = n + rstrB.Xr.Count + rstrB.Xl.Count;
                for (int i = 0; i < rstrA.Xl.Count; i++) pairs.Add((rstrA.Xl[i], 2));
                for (int i = 0; i < rstrA.Xr.Count; i++) pairs.Add((rstrA.Xr[i], -2));
                for (int i = 0; i < rstrB.Xl.Count; i++) pairs.Add((rstrB.Xl[i], 1));
                for (int i = 0; i < rstrB.Xr.Count; i++) pairs.Add((rstrB.Xr[i], -1));
                if (pairs.Count() > 1)
                {
                    // слияние элементов с одинаковой координатой по X
                    pairs = pairs.OrderBy(x => x.Item1).ToList();
                    (int, int) temp_item = pairs.First();
                    for (int i = 1; i < pairs.Count(); i++)
                    {
                        if (pairs[i].Item1 == temp_item.Item1)
                        {
                            pairs.Insert(i - 1, (pairs[i].Item1, pairs[i].Item2 + temp_item.Item2));
                            pairs.RemoveRange(i, 2);
                            i--;
                        }
                        temp_item = pairs[i];
                    }
                    // удаление дубликатов
                    temp_item = pairs.First();
                    for (int i = 1; i < pairs.Count(); i++)
                    {
                        if (pairs[i] == temp_item)
                        {
                            pairs.Insert(i - 1, (pairs[i].Item1, pairs[i].Item2));
                            pairs.RemoveRange(i, 2);
                            i--;
                        }
                        temp_item = pairs[i];
                    }
                }
                int Q = 0, Qnew = 0;
                for (int i = 0; i < pairs.Count(); i++)
                {
                    int x = pairs.ElementAt(i).Item1;
                    Qnew = Q + pairs.ElementAt(i).Item2;

                    if ((in_set(SetQ, Q) == false) && in_set(SetQ, Qnew)) 
                    {
                        tmo_result[str].Xl.Add(x);
                    }
                    if (in_set(SetQ, Q) && (in_set(SetQ, Qnew) == false))
                    {
                        tmo_result[str].Xr.Add(x);
                    }
                    Q = Qnew;
                }
            }
            return tmo_result;
        }
        bool in_set(int[] arr, int value)
        {
            value = Math.Abs(value);
            if (value >= arr[0] && value <= arr[1]) return true;
            return false;
        }
        // заливка Bitmap белым цветом (фон изображения)
        private void White(ref Bitmap src)
        {
            for (int j = 0; j < src.Height; j++)
            {
                for (int i = 0; i < src.Width; i++)
                {
                    src.SetPixel(i, j, System.Drawing.Color.White);
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;

namespace TMO
{
    // класс для работы с данными многоугольника
    public partial class Polygon
    {
        // диапазон попадания мышью в точку при редактировании
        public static int hit_range = 15;
        // радиус точки при отрисовке
        public static int point_radius = 3;

        // список координат вершин многоугольника
        public List<Point> points;
        // список точек для закрашивания (результат вычислений)
        public List<Point> pixel_coords;
        // цвет, тип
        public Color color;
        // флаг, обозначающий, что происходит задание точки
        public bool holded = false;
        public Raster raster;

        public Polygon()
        {
            points = new List<Point>();
            pixel_coords = new List<Point>();
            raster = new Raster();
        }
        public Polygon(Color color) : this()
        {
            this.color = color;
        }
        public Polygon Copy()
        {
            Polygon polygon = new Polygon(color);
            foreach (Point p in points)
            {
                polygon.points.Add(new Point(p.X, p.Y));
            }

            return polygon;
        }
        // заполнение списка с координатами точек для закрашивания
        public void Process(bool conditions)
        {
            pixel_coords.Clear();
            // выделение точек, по которым строится кривая
            DrawPoints();
            if (points.Count == 0) return;
            // закрашивание многоугольника
            if (points.Count > 2) PolygonPaint();
            // отрисовка границ многоугольника
            if (conditions)
            {
                DrawBorders();
            }
        }
        private void DrawPoints()
        {
            foreach (var point in points)
            {
                // закрашивание круга заданного радиуса
                BresenhamCircle(point.X, point.Y, point_radius);
            }
        }
        // проверка, есть ли точки кривой в заданном радиусе от позиции мыши
        public int IsPointNearby(Point mouse)
        {
            for (int i = 0; i < points.Count; i++)
            {
                if (Math.Abs(mouse.X - points[i].X) < hit_range &&
                    Math.Abs(mouse.Y - points[i].Y) < hit_range) return i;
            }
            return -1;
        }
        private void PolygonPaint()
        {
            PointsTranslate();
            List<Point> sorted_points = new List<Point>(points.OrderBy(x => x.Y));
            Point highest = sorted_points.Last();
            int n = points.Count;
            int Ymax_ind = points.IndexOf(highest);
            int Ymin = sorted_points.First().Y, Ymax = highest.Y;
            bool CW = false;
            double det = Determinant(new double[][] {
                new double[] {points[ind_(Ymax_ind - 1)].X, points[ind_(Ymax_ind - 1)].Y, 1},
                new double[] {points[Ymax_ind].X, points[Ymax_ind].Y, 1},
                new double[] {points[ind_(Ymax_ind + 1)].X, points[ind_(Ymax_ind + 1)].Y, 1}
            });
            if (det == 0)
            {
                // горизонтальная линия
                return;
            }
            if (det < 0) CW = true;
            raster = new Raster(Frame.default_canvas_height, Frame.default_canvas_width);
            if (CW == true) raster.FillRange(0, Ymin);
            for (int Y = Ymin; Y <= Ymax; Y++)
            {
                int k = 0;
                RasterString Ystring = raster[Y];
                for (int i = 0; i < n; i++)
                {
                    if (i < n - 1) k = i + 1;
                    else k = 0;
                    Point Pk = points[k], Pi = points[i];
                    if ((Pi.Y < Y && Pk.Y >= Y) || (Pi.Y >= Y && Pk.Y < Y))
                    {
                        int x = CrossSection(Pi, Pk, Y);
                        if (Pk.Y - Pi.Y > 0) Ystring.Xr.Add(x);
                        else Ystring.Xl.Add(x);
                    }
                }
                if (CW == true)
                {
                    Ystring.Xl.Add(0);
                    Ystring.Xr.Add(raster.width - 1);
                }
                Ystring.Sort();
            }
            if (CW == true) raster.FillRange(Ymax, raster.height);
            PointsTranslate();
        }

        int ind_(int val)
        {
            if (val >= points.Count) return 0;
            if (val < 0) return points.Count - 1;
            return val;
        }

        // получение координаты по Х пересечения строки с координатой Y и отрезка, проходящего через р1 и р2
        int CrossSection(Point p1, Point p2, int y)
        {
            if (p2.Y == p1.Y) return -1;
            return (y - p1.Y) * (p2.X - p1.X) / (p2.Y - p1.Y) + p1.X;

        }
        // возвращает определитель матрицы
        double Determinant(double[][] a)
        {
            int n = 3;
            const double EPS = 1E-9;
            //double[][] a = new double[n][];
            double[][] b = new double[1][];
            b[0] = new double[n];
            double det = 1;
            //проходим по строкам
            for (int i = 0; i < n; ++i)
            {
                //присваиваем k номер строки
                int k = i;
                //идем по строке от i+1 до конца
                for (int j = i + 1; j < n; ++j)
                    //проверяем
                    if (Math.Abs(a[j][i]) > Math.Abs(a[k][i]))
                        //если равенство выполняется то k присваиваем j
                        k = j;
                //если равенство выполняется то определитель приравниваем 0 и выходим из программы
                if (Math.Abs(a[k][i]) < EPS)
                {
                    det = 0;
                    break;
                }
                //меняем местами a[i] и a[k]
                b[0] = a[i];
                a[i] = a[k];
                a[k] = b[0];
                //если i не равно k
                if (i != k)
                    //то меняем знак определителя
                    det = -det;
                //умножаем det на элемент a[i][i]
                det *= a[i][i];
                //идем по строке от i+1 до конца
                for (int j = i + 1; j < n; ++j)
                    //каждый элемент делим на a[i][i]
                    a[i][j] /= a[i][i];
                //идем по столбцам
                for (int j = 0; j < n; ++j)
                    //проверяем
                    if ((j != i) && (Math.Abs(a[j][i]) > EPS))
                        //если да, то идем по k от i+1
                        for (k = i + 1; k < n; ++k)
                            a[j][k] -= a[i][k] * a[j][i];
            }
            //выводим результат
            return det;
        }

        // инверсия координат по оси Y
        void PointsTranslate()
        {
            for (int i = 0; i < points.Count; i++)
            {
                points.Insert(i, new Point(points[i].X, Frame.default_canvas_height - points[i].Y - 1));
                points.RemoveAt(i + 1);
            }
        }

        // отрисовка линии
        void line(int x0, int y0, int x1, int y1)
        {
            var steep = Math.Abs(y1 - y0) > Math.Abs(x1 - x0); // Проверяем рост отрезка по оси икс и по оси игрек
                                                               // Отражаем линию по диагонали, если угол наклона слишком большой
            if (steep)
            {
                Swap(ref x0, ref y0); // Перетасовка координат вынесена в отдельную функцию для красоты
                Swap(ref x1, ref y1);
            }
            // Если линия растёт не слева направо, то меняем начало и конец отрезка местами
            if (x0 > x1)
            {
                Swap(ref x0, ref x1);
                Swap(ref y0, ref y1);
            }
            int dx = x1 - x0;
            int dy = Math.Abs(y1 - y0);
            int error = dx / 2; // Здесь используется оптимизация с умножением на dx, чтобы избавиться от лишних дробей
            int ystep = (y0 < y1) ? 1 : -1; // Выбираем направление роста координаты y
            int y = y0;
            for (int x = x0; x <= x1; x++)
            {
                pixel_coords.Add(new Point(steep ? y : x, steep ? x : y)); // Не забываем вернуть координаты на место
                error -= dy;
                if (error < 0)
                {
                    y += ystep;
                    error += dx;
                }
            }
        }
        void Swap(ref int a, ref int b)
        {
            int temp = a;
            a = b;
            b = temp;
        }
        // закрашивание круга
        void BresenhamCircle(int cx, int cy, int radius)
        {
            int x = radius, y = 0;
            int D = 2 * (1 - radius);

            while (x >= 0)
            {
                line(cx - x, cy + y, cx + x, cy + y);
                line(cx - x, cy - y, cx + x, cy - y);

                if (D < 0 && 2 * D + 2 * x - 1 <= 0)
                {
                    ++y;
                    D += 2 * y + 1;
                }
                else if (D > 0 && 2 * D - 2 * y - 1 >= 0)
                {
                    --x;
                    D -= 2 * x - 1;
                }
                else
                {
                    --x;
                    ++y;
                    D += 2 * y - 2 * x + 2;
                }
            }
        }
        // отрисовка границ многоугольника
        void DrawBorders()
        {
            for (int i = 1; i < points.Count; i++)
            {
                line(points[i].X, points[i].Y, points[i - 1].X, points[i - 1].Y);
            }
            line(points[0].X, points[0].Y, points[points.Count - 1].X, points[points.Count - 1].Y);
        }
    }
}

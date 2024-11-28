using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace TMO
{
    internal class PolygonVisualiser
    {
        // ссылки на объекты интерфейса управления
        public static System.Windows.Controls.Image image;
        public static System.Windows.Controls.ComboBox polygon_type;
        public static System.Windows.Controls.ComboBox polygon_color;
        // список цветов отображаемых кривых
        public static List<Color> colors = new List<Color>() { Color.LimeGreen, Color.Red, Color.DarkOrange, Color.Yellow,
             Color.SkyBlue, Color.Blue, Color.DarkViolet };

        // список ссылок на объекты класса Frame (служит для функций Redo-Undo)
        public List<Frame> frames = new List<Frame>();
        // позиция текущего кадра относительно конца списка кадров
        public int frame_shift = 0;
        // индекс точки, редактируемой при помощи мыши
        public int edit_point_index = 0;
        // индекс фигуры, редактируемой при помощи мыши
        public int edit_curve_index = 0;
        // флаг режима отображения условий
        public bool show_condidtions = false;
        // флаг режима заливки многоугольников
        public bool fill_polygons = true;
        // тип выбранной ТМО
        public TMOtype tmo_type;

        // свойство, возвращающее ссылку на текущий кадр
        public Frame current_frame
        {
            get
            {
                if (frames.Count == 0) frames.Add(new Frame());
                return frames[frames.Count - 1 - frame_shift];
            }
        }
        public PolygonVisualiser()
        {
            DrawImage();
        }

        public void DrawImage()
        {
            image.Source = current_frame.Draw(show_condidtions, fill_polygons, tmo_type);
        }
        // задание новой точки
        public void SetNewPoint(Point point, bool release)
        {
            if (current_frame.polygons.Count > 2) return;
            // создание новой кривой, если в текущем кадре кривых нет
            if (current_frame.polygons.Count == 0)
                current_frame.polygons.Add(new Polygon(colors[polygon_color.SelectedIndex]));
            // создание ссылки на текущую кривую
            Polygon target = current_frame.active_polygon;
            //// ограничение количества точек у многоугольника
            //if (current_frame.active_curve.type == PolygonType.orient
            //    && current_frame.active_curve.points.Count > 3
            //    && current_frame.active_curve.holded == false) return;
            // если клавиша мыши была отпущена, в кривую добавляется новая точка 
            if (release)
            {
                // снимаем флаг удержания
                target.holded = false;
                // удаляем "плавающую" точку
                target.points.Remove(target.points.Last());
                // переходим к новому кадру
                NewFrame();
                // обновляем ссылку, чтобы она указывала на новый (активный) кадр
                target = current_frame.active_polygon;
                // добавляем в новую кривую нового кадра новую точку
                target.points.Add(new Point(point.X, point.Y));
            }
            // если клавиша удерживается, заменяется старая
            else
            {
                // если редактирование новой точки ещё не началось (момент нажатия), ставим флаг удержания
                if (!target.holded) target.holded = true;
                else
                {
                    // в процессе удержания - сначала удаляем последнюю точку
                    if (target.points.Count > 0) target.points.Remove(target.points.Last());
                }
                // добавляем новую точку
                target.points.Add(new Point(point.X, point.Y));
            }
            DrawImage();
        }
        // изменение положения точки
        public void ChangePoint(Point point, int point_index)
        {
            Point new_point = new Point(point.X, point.Y);
            current_frame.polygons[edit_curve_index].points.RemoveAt(point_index);
            current_frame.polygons[edit_curve_index].points.Insert(point_index, new_point);
            DrawImage();
        }
        // переход к новой кривой
        public void NewCurve()
        {
            // обработка случая, когда в текущей кривой нет точек
            if (current_frame.polygons.Last().points.Count() == 0) return;
            // добавление ссылки на новую кривую в список
            current_frame.polygons.Add(new Polygon(colors[polygon_color.SelectedIndex]));
            // смена цвета новой кривой
            if (polygon_color.SelectedIndex == polygon_color.Items.Count - 1) polygon_color.SelectedIndex = 0;
            else polygon_color.SelectedIndex++;
        }
        // функция "Отменить"
        public void Undo()
        {
            // изменение значения смещения текущего кадра
            frame_shift++;
            if (frame_shift > frames.Count - 1) frame_shift = frames.Count - 1;
            if (current_frame.polygons.Count > 0)
            {
                // блокировка вызова обработчиков событий CheckBox
                BlockEvents(() =>
                {
                    // обновление содержимого CheckBox-ов
                    polygon_color.SelectedIndex = colors.IndexOf(current_frame.active_polygon.color);
                    //curve_type.SelectedIndex = (int)current_frame.active_curve.type;
                });
            }
            DrawImage();
        }
        // функция "Повторить"
        public void Redo()
        {
            frame_shift--;
            if (frame_shift < 0) frame_shift = 0;
            if (current_frame.polygons.Count > 0)
            {
                BlockEvents(() =>
                {
                    polygon_color.SelectedIndex = colors.IndexOf(current_frame.active_polygon.color);
                });
            }
            DrawImage();
        }
        // переход к новому кадру
        public void NewFrame()
        {
            // если ранее была вызвана функция "Отменить", последующие кадры удаляются
            if (frame_shift > 0)
            {
                frames = frames.GetRange(0, frames.Count - frame_shift);
                frame_shift = 0;
            }
            // добавление ссылки на новый кадр в список
            frames.Add(new Frame(current_frame));
        }
        private void BlockEvents(Action act)
        {
            MainWindow.events_locked = true;
            act();
            MainWindow.events_locked = false;
        }
    }
    delegate void Action();
}

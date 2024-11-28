using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Timers;
using System.Security.Policy;

namespace TMO
{
    public partial class MainWindow : Window
    {
        // размер области рисования (зависит от размера окна)
        public static double canvas_height = 0;
        public static double canvas_width = 0;

        // флаг запрета обработки событий CheckBox
        public static bool events_locked = false;
        // флаг удержания ЛКМ
        bool mouse_left_hold = false;
        // флаг удержания ПКМ
        bool mouse_right_hold = false;
        // флаг удержания CTRL
        bool ctrl_hold = false;
        // ссылка на объект класса PolygonVisualiser
        PolygonVisualiser canvas;

        public static Timer aTimer = new Timer(5);
        private static bool fps_flag = false;

        public MainWindow()
        {
            InitializeComponent();
            canvas_width = image.ActualWidth;
            canvas_height = image.ActualHeight;
            PolygonVisualiser.image = image;
            PolygonVisualiser.polygon_type = TMOTypeBox;
            PolygonVisualiser.polygon_color = PColorBox;
            canvas = new PolygonVisualiser();
            PColorBox.ItemsSource = PolygonVisualiser.colors;
            PColorBox.SelectedIndex = 0;
            TMOTypeBox.ItemsSource = new string[]
            {
                "без ТМО", "объединение", "пересечение", "симм. разн.", "А-В", "В-А"
            };
            TMOTypeBox.SelectedIndex = 0;
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = false;
            aTimer.Enabled = true;
        }

        private static void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            fps_flag = true;
        }
        // нажатие кнопки очистки холста
        private void clearButton_Click(object sender, RoutedEventArgs e)
        {
            canvas = new PolygonVisualiser();
        }
        // нажатие кнопки отображения условий
        private void borderButton_Click(object sender, RoutedEventArgs e)
        {
            canvas.show_condidtions = !canvas.show_condidtions;
            if (canvas.show_condidtions) borderButton.Content = "выкл. границы";
            else borderButton.Content = "вкл. границы";
            canvas.DrawImage();
        }
        // нажатие кнопки заливки
        private void fillButton_Click(object sender, RoutedEventArgs e)
        {
            canvas.fill_polygons = !canvas.fill_polygons;
            if (canvas.fill_polygons) fillButton.Content = "выкл. заливку";
            else fillButton.Content = "вкл. заливку";
            canvas.DrawImage();
        }
        // нажатие клавиши при активном окне
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.N) canvas.NewCurve();
            if (e.Key == Key.LeftCtrl) ctrl_hold = true;
            if (e.Key == Key.Z && ctrl_hold == true) canvas.Undo();
            if (e.Key == Key.Y && ctrl_hold == true) canvas.Redo();
        }
        // отпускание клавиши
        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftCtrl) ctrl_hold = false;
        }
        // события ЛЕВОЙ кнопки мыши (задание новой точки)
        private void image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            mouse_left_hold = true;
            canvas.SetNewPoint(pixel_position(), false);
        }
        private void image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            mouse_left_hold = false;
            canvas.SetNewPoint(pixel_position(), true);
        }
        // события ПРАВОЙ кнопки мыши (редактирование заданной точки)
        private void image_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (canvas.current_frame.polygons.Count == 0) return;
            canvas.edit_point_index = -1;
            System.Drawing.Point cursor = pixel_position();
            foreach (var polygon in canvas.current_frame.polygons)
            {
                int result = polygon.IsPointNearby(cursor);
                if (result >= 0)
                {
                    canvas.edit_point_index = result;
                    canvas.edit_curve_index = canvas.current_frame.polygons.IndexOf(polygon);
                    mouse_right_hold = true;
                    canvas.NewFrame();
                    canvas.ChangePoint(cursor, canvas.edit_point_index);
                    return;
                }
            }
            mouse_right_hold = false;
        }
        private void image_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (mouse_right_hold == true)
            {
                canvas.ChangePoint(pixel_position(), canvas.edit_point_index);
                mouse_right_hold = false;
            }
        }
        // движение мыши по области рисования
        private void image_MouseMove(object sender, MouseEventArgs e)
        {
            if (!fps_flag) return;
            fps_flag = false;
            aTimer.Start();
            if (mouse_left_hold) canvas.SetNewPoint(pixel_position(), false);
            else if (mouse_right_hold) canvas.ChangePoint(pixel_position(), canvas.edit_point_index);
        }
        // изменение размера окна
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // расчёт высоты и ширины области рисования
            canvas_width = image.ActualWidth;
            canvas_height = image.ActualHeight;
        }
        // изменение цвета многоугольника
        private void PColorBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (events_locked) return;
            Polygon polygon = canvas.current_frame.active_polygon;
            if (polygon == null) return;
            if (polygon.points.Count > 0) canvas.NewFrame();
            canvas.current_frame.active_polygon.color = PolygonVisualiser.colors[PColorBox.SelectedIndex];
            canvas.DrawImage();
        }
        // изменение типа ТМО
        private void TMOTypeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (events_locked) return;
            Polygon polygon = canvas.current_frame.active_polygon;
            if (polygon == null) return;
            canvas.tmo_type = (TMOtype)TMOTypeBox.SelectedIndex;
            canvas.DrawImage();
        }
        // получение позиции мыши на области рисования
        private System.Drawing.Point pixel_position()
        {
            System.Windows.Point position = Mouse.GetPosition(image);
            System.Drawing.Point pos_pixel = new System.Drawing.Point((int)(position.X * Frame.default_canvas_width / canvas_width),
                (int)(position.Y * Frame.default_canvas_height / canvas_height));

            return pos_pixel;
        }
    }
}

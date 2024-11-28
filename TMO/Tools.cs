using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TMO
{ 
    public class Tools
    {
        // вывод окна с сообщением
        public static void message(string msg)
        {
            string caption = "сообщение";
            MessageBoxButton button = MessageBoxButton.OK;
            MessageBoxImage icon = MessageBoxImage.Information;
            MessageBoxResult result;

            result = MessageBox.Show(msg, caption, button, icon, MessageBoxResult.Yes);
        }
    }
}

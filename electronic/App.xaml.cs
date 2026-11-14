using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace electronic
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            // Подписываемся на глобальный перехват ошибок
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            // Выводим дружелюбное сообщение
            MessageBox.Show("Упс! Что-то пошло не так.\n\nДетали: " + e.Exception.Message,
                            "Системное уведомление",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);

            // Сообщаем системе, что мы обработали ошибку, чтобы приложение не вылетело
            e.Handled = true;
        }
    }
}

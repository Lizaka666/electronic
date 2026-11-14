using electronic.Data;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace electronic.Pages
{
    public partial class ProfilePage : Page
    {
        public ProfilePage()
        {
            InitializeComponent();

            // Подгружаем текущие данные авторизованного пользователя
            if (AppConnect.user != null)
            {
                txtSurname.Text = AppConnect.user.surname;
                txtName.Text = AppConnect.user.name;
                txtLogin.Text = AppConnect.user.login;
                txtPassword.Text = AppConnect.user.password;
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            // 1. ВАЛИДАЦИЯ: Проверка на пустоту
            if (string.IsNullOrWhiteSpace(txtSurname.Text) ||
                string.IsNullOrWhiteSpace(txtName.Text) ||
                string.IsNullOrWhiteSpace(txtLogin.Text) ||
                string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show("Пожалуйста, заполните все поля.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. ВАЛИДАЦИЯ: Проверка уникальности логина (если юзер решил его поменять)
            string newLogin = txtLogin.Text.Trim();
            var duplicate = AppConnect.model1.User.FirstOrDefault(u => u.login == newLogin && u.id != AppConnect.user.id);
            if (duplicate != null)
            {
                MessageBox.Show("Этот логин уже занят другим пользователем!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // Находим пользователя в базе данных по его ID
                var userInDb = AppConnect.model1.User.FirstOrDefault(u => u.id == AppConnect.user.id);

                if (userInDb != null)
                {
                    // Обновляем данные в БД
                    userInDb.surname = txtSurname.Text.Trim();
                    userInDb.name = txtName.Text.Trim();
                    userInDb.login = newLogin;
                    userInDb.password = txtPassword.Text.Trim();

                    AppConnect.model1.SaveChanges();

                    // Обновляем данные в текущей сессии
                    AppConnect.user.surname = userInDb.surname;
                    AppConnect.user.name = userInDb.name;
                    AppConnect.user.login = userInDb.login;
                    AppConnect.user.password = userInDb.password;

                    // Обновляем ФИО в верхнем углу окна
                    var mw = Application.Current.MainWindow as MainWindow;
                    if (mw != null)
                    {
                        mw.FIO.Text = AppConnect.user.FIO;
                    }

                    MessageBox.Show("Ваши данные успешно обновлены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    NavigationService.GoBack();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения: " + ex.Message);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}
using electronic.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace electronic.Pages
{
    /// <summary>
    /// Логика взаимодействия для RegistrationPage.xaml
    /// </summary>
    public partial class RegistrationPage : Page
    {
        public RegistrationPage()
        {
            InitializeComponent();
        }

        private void btnRegister_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSurname.Text) ||
                string.IsNullOrWhiteSpace(txtName.Text) ||
                string.IsNullOrWhiteSpace(txtLogin.Text) ||
                string.IsNullOrWhiteSpace(pbPassword.Password))
            {
                MessageBox.Show("Поля не могут быть пустыми или состоять только из пробелов!");
                return;
            }

            // Обычно логин и имена в базе имеют ограничение varchar(50) или nvarchar(50)
            if (txtSurname.Text.Length > 50 || txtName.Text.Length > 50 || txtPatronymic.Text.Length > 50)
            {
                MessageBox.Show("ФИО слишком длинное! Максимум 50 символов на каждое поле.");
                return;
            }

            if (txtLogin.Text.Length > 50)
            {
                MessageBox.Show("Логин слишком длинный! Максимум 50 символов.");
                return;
            }
            // Проверка на пустые поля (Отчество не проверяем, оно может быть пустым)
            if (txtSurname.Text == "" || txtName.Text == "" || txtLogin.Text == "" || pbPassword.Password == "")
            {
                MessageBox.Show("Пожалуйста, заполните все обязательные поля!");
                return; // Останавливаем выполнение кода
            }

            // Проверка, что пароли совпадают
            if (pbPassword.Password != pbPasswordConfirm.Password)
            {
                MessageBox.Show("Пароли не совпадают. Проверьте правильность ввода!");
                return;
            }

            // Проверка уникальности логина (ищем в базе такого же юзера)
            var existingUser = AppConnect.model1.User.FirstOrDefault(u => u.login == txtLogin.Text);

            if (existingUser != null) // Если нашли, значит логин занят
            {
                MessageBox.Show("Такой логин уже существует. Придумайте другой!");
                return;
            }

            // Собираем нового пользователя из введенных данных
            User newUser = new User();
            newUser.surname = txtSurname.Text;
            newUser.name = txtName.Text;
            newUser.patronymic = txtPatronymic.Text;
            newUser.login = txtLogin.Text;
            newUser.password = pbPassword.Password;

            
            newUser.idRole = 3;

            // 5. Отправляем в базу
            try
            {
                AppConnect.model1.User.Add(newUser);
                AppConnect.model1.SaveChanges();

                MessageBox.Show("Вы успешно зарегистрированы!");

                // Возвращаемся обратно на страницу авторизации
                NavigationService.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Произошла ошибка при сохранении: " + ex.Message);
            }
        }

        // Кнопка возврата назад (отмена)
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}

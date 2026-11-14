using electronic.Data;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace electronic.Pages
{
    public partial class AddEditUserPage : Page
    {
        private User _currentUser;
        private bool _isNew = false;

        public AddEditUserPage(User selectedUser)
        {
            InitializeComponent();

            // Загружаем список ролей (таблица Role)
            cmbRole.ItemsSource = AppConnect.model1.Role.ToList();

            if (selectedUser == null)
            {
                _currentUser = new User();
                _isNew = true;
                txtTitle.Text = "Новый пользователь";
                cmbRole.SelectedIndex = 2; // По умолчанию 3-я роль (Авторизованный пользователь)
            }
            else
            {
                _currentUser = selectedUser;
                _isNew = false;
                txtTitle.Text = "Редактирование";

                txtSurname.Text = _currentUser.surname;
                txtName.Text = _currentUser.name;
                txtLogin.Text = _currentUser.login;
                txtPassword.Text = _currentUser.password;

                // Устанавливаем текущую роль в ComboBox
                cmbRole.SelectedItem = AppConnect.model1.Role.FirstOrDefault(r => r.id == _currentUser.idRole);
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            // 1. Проверка на пустоту обязательных полей
            if (string.IsNullOrWhiteSpace(txtSurname.Text) ||
                string.IsNullOrWhiteSpace(txtName.Text) ||
                string.IsNullOrWhiteSpace(txtLogin.Text) ||
                string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show("Пожалуйста, заполните все поля (Фамилия, Имя, Логин, Пароль).", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Проверка уникальности логина
            string currentLogin = txtLogin.Text.Trim();
            var duplicateUser = AppConnect.model1.User.FirstOrDefault(u => u.login == currentLogin && u.id != _currentUser.id);
            if (duplicateUser != null)
            {
                MessageBox.Show("Пользователь с таким логином уже существует! Придумайте другой.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 3. Проверка выбора роли
            if (cmbRole.SelectedItem == null)
            {
                MessageBox.Show("Выберите роль пользователя!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Применяем данные
                _currentUser.surname = txtSurname.Text.Trim();
                _currentUser.name = txtName.Text.Trim();
                _currentUser.login = currentLogin;
                _currentUser.password = txtPassword.Text.Trim();

                var selectedRole = cmbRole.SelectedItem as Role;
                _currentUser.idRole = selectedRole.id;

                if (_isNew)
                {
                    AppConnect.model1.User.Add(_currentUser);
                }

                AppConnect.model1.SaveChanges();
                MessageBox.Show("Пользователь успешно сохранен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                NavigationService.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка базы данных: " + ex.Message);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}
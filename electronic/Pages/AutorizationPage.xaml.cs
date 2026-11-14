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
    /// Логика взаимодействия для AutorizationPage.xaml
    /// </summary>
    public partial class AutorizationPage : Page
    {
        public AutorizationPage()
        {
            InitializeComponent();
            this.IsVisibleChanged += Page_IsVisibleChanged;
        }

        private void Page_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Как только страница снова появляется на экране - стираем данные
            if (Visibility == Visibility.Visible)
            {
                txtLogin.Text = "";

                txtPassword.Password = "";
            }
        }
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            AppConnect.user = null;
            AppConnect.mainFrame.Navigate(new ProductPage());
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string login = txtLogin.Text.Trim();
            string password = txtPassword.Password.Trim();

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Не корректный логин или пароль");
                return;
            }

            var u = AppConnect.model1.User.FirstOrDefault(x => x.login == login && x.password == password);
            if (u == null)
            {
                MessageBox.Show("Пользователь не найден");
                return;
            }
            AppConnect.user = u;
            AppConnect.mainFrame.Navigate(new ProductPage());
        }

        private void btnReg_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new RegistrationPage());
        }
    }
}

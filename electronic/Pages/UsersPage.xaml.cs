using electronic.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace electronic.Pages
{
    public partial class UsersPage : Page
    {
        public class UserView
        {
            public User UserRecord { get; set; }
            public string FullName => $"{UserRecord.surname} {UserRecord.name}";
            public string RoleName { get; set; }
        }

        public UsersPage()
        {
            InitializeComponent();
            IsVisibleChanged += Page_IsVisibleChanged;
        }

        private void Page_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Visibility == Visibility.Visible)
            {
                LoadUsers();
            }
        }

        private void LoadUsers()
        {
            var users = AppConnect.model1.User.ToList();
            var displayList = new List<UserView>();

            foreach (var u in users)
            {
                var role = AppConnect.model1.Role.FirstOrDefault(r => r.id == u.idRole);
                displayList.Add(new UserView
                {
                    UserRecord = u,
                    RoleName = role != null ? role.role1 : "Неизвестная роль"
                });
            }

            listUsers.ItemsSource = displayList;
        }

        private void btnAddUser_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new AddEditUserPage(null));
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var userView = btn.DataContext as UserView;
            NavigationService.Navigate(new AddEditUserPage(userView.UserRecord));
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var userView = btn.DataContext as UserView;
            var userToDelete = userView.UserRecord;

            // Нельзя удалить самого себя
            if (userToDelete.id == AppConnect.user.id)
            {
                MessageBox.Show("Вы не можете удалить свою собственную учетную запись!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            //Нельзя удалить пользователя, если у него есть заказы
            bool hasOrders = AppConnect.model1.Order.Any(o => o.idUser == userToDelete.id);
            if (hasOrders)
            {
                MessageBox.Show("Невозможно удалить пользователя, так как у него есть оформленные заказы. Это нарушит целостность базы данных.", "Контроль целостности", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"Вы точно хотите удалить пользователя {userView.FullName}?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    AppConnect.model1.User.Remove(userToDelete);
                    AppConnect.model1.SaveChanges();
                    MessageBox.Show("Пользователь удален.");
                    LoadUsers();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при удалении: " + ex.Message);
                }
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}
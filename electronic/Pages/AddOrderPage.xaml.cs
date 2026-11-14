using electronic.Data;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace electronic.Pages
{
    public partial class AddOrderPage : Page
    {
        // Обертка для красивого вывода ФИО клиента
        public class UserView
        {
            public User UserRecord { get; set; }
            public string FullName => $"{UserRecord.surname} {UserRecord.name}";
        }

        public AddOrderPage()
        {
            InitializeComponent();

            // Загружаем только клиентов
            var clients = AppConnect.model1.User.Where(u => u.idRole == 3).ToList();

            // Оборачиваем их в наш класс, чтобы комбобокс показал и Имя, и Фамилию
            cmbUsers.ItemsSource = clients.Select(c => new UserView { UserRecord = c }).ToList();
        }

        private void btnCreate_Click(object sender, RoutedEventArgs e)
        {
            if (cmbUsers.SelectedItem is UserView selectedClient)
            {
                try
                {
                    Random rnd = new Random();

                    // Создаем абсолютно пустой заказ
                    Order newOrder = new Order
                    {
                        idOrder = rnd.Next(100000, 999999),
                        date = DateTime.Now,
                        idUser = selectedClient.UserRecord.id,
                        idStatusOrder = 4
                    };

                    AppConnect.model1.Order.Add(newOrder);
                    AppConnect.model1.SaveChanges(); // Сохраняем в БД, чтобы получить системный ID

                    // Удаляем эту страницу создания из истории, чтобы при нажатии "Назад" 
                    // менеджер не возвращался к выбору клиента
                    NavigationService.RemoveBackEntry();

                    // Прыгаем на нашу крутую страницу редактирования с новым заказом!
                    NavigationService.Navigate(new EditOrderPage(newOrder));
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка создания заказа: " + ex.Message);
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите клиента из списка.");
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}
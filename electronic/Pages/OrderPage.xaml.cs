using electronic.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace electronic.Pages
{
    public partial class OrderPage : Page
    {
        // Класс-обертка для красивого вывода данных заказа на экран
        public class OrderView
        {
            public Visibility IsEditVisible => (AppConnect.user.idRole == 1 || AppConnect.user.idRole == 2) ? Visibility.Visible : Visibility.Collapsed;
            public Visibility IsDeleteVisible => AppConnect.user.idRole == 1 ? Visibility.Visible : Visibility.Collapsed;
            public Visibility IsCancelVisible => OrderRecord.idStatusOrder == 4 ? Visibility.Visible : Visibility.Collapsed;
            public Order OrderRecord { get; set; }

            // Номер чека (который мы генерировали рандомом)
            public string OrderTitle => $"Заказ №{OrderRecord.idOrder}";

            public string OrderDate => OrderRecord.date.ToString("dd.MM.yyyy");

            public decimal TotalSum { get; set; }

            public string StatusName { get; set; }

            public string ClientInfo { get; set; } // Для вывода ФИО клиента админам

            // Скрываем ФИО клиента, если зашел обычный покупатель
            public Visibility IsAdminInfoVisible => (AppConnect.user.idRole == 1 || AppConnect.user.idRole == 2) ? Visibility.Visible : Visibility.Collapsed;
        }

        public OrderPage()
        {
            InitializeComponent();
            LoadOrders();
        }

        private void LoadOrders()
        {
            if (AppConnect.user == null) return;

            List<Order> rawOrders = new List<Order>();

            // ПРОВЕРКА РОЛЕЙ
            if (AppConnect.user.idRole == 1 || AppConnect.user.idRole == 2)
            {
                btnAddOrder.Visibility = Visibility.Visible; // Показываем кнопку сотрудникам
                rawOrders = AppConnect.model1.Order.OrderByDescending(o => o.date).ToList();
            }
            else
            {
                btnAddOrder.Visibility = Visibility.Collapsed; // Прячем от обычных клиентов
                rawOrders = AppConnect.model1.Order.Where(o => o.idUser == AppConnect.user.id)
                                                   .OrderByDescending(o => o.date).ToList();
            }

            List<OrderView> displayList = new List<OrderView>();

            // ФОРМИРОВАНИЕ СПИСКА
            foreach (var order in rawOrders)
            {
                OrderView view = new OrderView();
                view.OrderRecord = order;

                // Подсчитываем сумму товаров в этом заказе через таблицу OrderStock
                var orderItems = AppConnect.model1.OrderStock.Where(os => os.idOrder == order.id).ToList();
                decimal sum = 0;
                foreach (var item in orderItems)
                {
                    if (item.price != null && item.count != null)
                    {
                        sum += (decimal)item.price * (int)item.count;
                    }
                }
                view.TotalSum = sum;

                var statusObj = AppConnect.model1.StatusOrder.FirstOrDefault(s => s.id == order.idStatusOrder);

                if (statusObj != null)
                {
                    view.StatusName = statusObj.Status;
                }
                else
                {
                    view.StatusName = "Ошибка статуса";
                }

                // Подтягиваем данные клиента для админов
                var client = AppConnect.model1.User.FirstOrDefault(u => u.id == order.idUser);
                if (client != null)
                {
                    view.ClientInfo = $"Клиент: {client.surname} {client.name}";
                }

                displayList.Add(view);
            }

            listOrders.ItemsSource = displayList;
        }
        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (AppConnect.user?.idRole != 1)
            {
                MessageBox.Show("Удаление доступно только администратору!");
                return;
            }

            var btn = sender as Button;
            var orderView = btn.DataContext as OrderView;
            var order = orderView.OrderRecord;

            if (MessageBox.Show($"Вы точно хотите удалить {orderView.OrderTitle}?\nВсе товары из этого заказа будут возвращены на склад.", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    // Возвращаем товары на склад и удаляем привязки из склада
                    var orderItems = AppConnect.model1.OrderStock.Where(os => os.idOrder == order.id).ToList();

                    // статус НЕ "Отменён" товары всё еще в резерве за этим заказом их надо вернуть
                    if (order.idStatusOrder != 3)
                    {
                        foreach (var item in orderItems)
                        {
                            var stock = AppConnect.model1.Stock.FirstOrDefault(s => s.id == item.idStock);
                            if (stock != null) stock.count += item.count;
                        }
                    }

                    //удалять связи из OrderStock перед удалением самого заказа
                    foreach (var item in orderItems)
                    {
                        AppConnect.model1.OrderStock.Remove(item);
                    }

                    // Теперь безопасно удаляем сам заказ (целостность не нарушена)
                    AppConnect.model1.Order.Remove(order);
                    AppConnect.model1.SaveChanges();

                    MessageBox.Show("Заказ удален, товары успешно возвращены на склад.");
                    LoadOrders(); // Перерисовываем список
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при удалении: " + ex.Message);
                }
            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var orderView = btn.DataContext as OrderView;
            var order = orderView.OrderRecord;

            // Переходим на страницу изменения статуса
            NavigationService.Navigate(new EditOrderPage(order));
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void btnAddOrder_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new AddOrderPage());
        }

        private void btnCancelOrder_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var orderView = btn.DataContext as OrderView;
            var order = orderView.OrderRecord;

            if (MessageBox.Show($"Вы уверены, что хотите отменить {orderView.OrderTitle}?\nТовары будут возвращены на склад.", "Подтверждение отмены", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    // Возвращаем товары на склад
                    var orderItems = AppConnect.model1.OrderStock.Where(os => os.idOrder == order.id).ToList();
                    foreach (var item in orderItems)
                    {
                        var stock = AppConnect.model1.Stock.FirstOrDefault(s => s.id == item.idStock);
                        if (stock != null)
                        {
                            stock.count += item.count; // Плюсуем обратно на склад
                        }
                    }

                    // Меняем статус заказа на "Отменён"
                    order.idStatusOrder = 3;

                    AppConnect.model1.SaveChanges();

                    MessageBox.Show("Заказ успешно отменён. Товары возвращены на склад.");
                    LoadOrders(); // Перерисовываем список
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при отмене заказа: " + ex.Message);
                }
            }
        }
    }
}
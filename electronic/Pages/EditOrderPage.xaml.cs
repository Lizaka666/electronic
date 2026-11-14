using electronic.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace electronic.Pages
{
    public partial class EditOrderPage : Page
    {
        private Order _currentOrder;
        private List<OrderItemView> _currentItems;

        // Обертка для товаров В ЗАКАЗЕ
        public class OrderItemView
        {
            public OrderStock OsRecord { get; set; }
            public string DisplayName { get; set; }
            public decimal EditPrice { get; set; } // Для редактирования цены в TextBox
            public int CurrentCount => OsRecord.count;
        }

        // Обертка для товаров НА СКЛАДЕ (для ComboBox добавления)
        public class StockAvailableView
        {
            public Stock StockRecord { get; set; }
            public string DisplayName { get; set; }
        }

        public EditOrderPage(Order selectedOrder)
        {
            InitializeComponent();
            _currentOrder = selectedOrder;

            txtOrderTitle.Text = $"Заказ №{_currentOrder.idOrder}";

            // Загрузка статусов
            cmbStatus.ItemsSource = AppConnect.model1.StatusOrder.ToList();
            cmbStatus.SelectedItem = AppConnect.model1.StatusOrder.FirstOrDefault(s => s.id == _currentOrder.idStatusOrder);

            LoadOrderItems();
            LoadAvailableStock();
        }

        // --- 1. ЗАГРУЗКА ДАННЫХ ---

        private void LoadOrderItems()
        {
            _currentItems = new List<OrderItemView>();
            var itemsInDb = AppConnect.model1.OrderStock.Where(os => os.idOrder == _currentOrder.id).ToList();

            foreach (var item in itemsInDb)
            {
                var stock = AppConnect.model1.Stock.FirstOrDefault(s => s.id == item.idStock);
                var product = AppConnect.model1.Product.FirstOrDefault(p => p.id == stock.idProduct);

                string sizeStr = "";
                if (stock.idSize != null)
                {
                    var size = AppConnect.model1.Size.FirstOrDefault(z => z.id == stock.idSize);
                    if (size != null) sizeStr = $" ({size.sizeGb} GB)";
                }

                _currentItems.Add(new OrderItemView
                {
                    OsRecord = item,
                    DisplayName = $"{product.name}{sizeStr}",
                    EditPrice = (decimal)item.price
                });
            }
            listOrderItems.ItemsSource = null;
            listOrderItems.ItemsSource = _currentItems;
        }

        private void LoadAvailableStock()
        {
            var stockList = new List<StockAvailableView>();
            // Берем только те товары, которые есть в наличии
            var availableStock = AppConnect.model1.Stock.Where(s => s.count > 0).ToList();

            foreach (var stock in availableStock)
            {
                var product = AppConnect.model1.Product.FirstOrDefault(p => p.id == stock.idProduct);
                string sizeStr = stock.idSize != null ? $" ({AppConnect.model1.Size.FirstOrDefault(z => z.id == stock.idSize)?.sizeGb} GB)" : "";

                stockList.Add(new StockAvailableView
                {
                    StockRecord = stock,
                    DisplayName = $"{product.name}{sizeStr} — В наличии: {stock.count} шт. [{(decimal)product.price} ₽]"
                });
            }
            cmbAddStock.ItemsSource = stockList;
        }


        // --- 2. ИЗМЕНЕНИЕ КОЛИЧЕСТВА (Плюс/Минус) ---

        private void btnPlus_Click(object sender, RoutedEventArgs e)
        {
            var view = (sender as Button).DataContext as OrderItemView;
            var stock = AppConnect.model1.Stock.FirstOrDefault(s => s.id == view.OsRecord.idStock);

            if (stock != null && stock.count > 0)
            {
                view.OsRecord.count++;
                stock.count--; // Забираем со склада
                AppConnect.model1.SaveChanges();
                LoadOrderItems();
                LoadAvailableStock();
            }
            else
            {
                MessageBox.Show("На складе больше нет этого товара!");
            }
        }

        private void btnMinus_Click(object sender, RoutedEventArgs e)
        {
            var view = (sender as Button).DataContext as OrderItemView;
            var stock = AppConnect.model1.Stock.FirstOrDefault(s => s.id == view.OsRecord.idStock);

            if (view.OsRecord.count > 1)
            {
                view.OsRecord.count--;
                if (stock != null) stock.count++; // Возвращаем на склад
                AppConnect.model1.SaveChanges();
                LoadOrderItems();
                LoadAvailableStock();
            }
        }


        // --- 3. УДАЛЕНИЕ И ДОБАВЛЕНИЕ ТОВАРОВ ---

        private void btnRemoveItem_Click(object sender, RoutedEventArgs e)
        {
            var view = (sender as Button).DataContext as OrderItemView;
            var stock = AppConnect.model1.Stock.FirstOrDefault(s => s.id == view.OsRecord.idStock);

            if (MessageBox.Show($"Удалить {view.DisplayName} из заказа?", "Удаление", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                // Возвращаем все штуки обратно на склад
                if (stock != null) stock.count += view.OsRecord.count;

                AppConnect.model1.OrderStock.Remove(view.OsRecord);
                AppConnect.model1.SaveChanges();

                LoadOrderItems();
                LoadAvailableStock();
            }
        }

        private void btnAddItem_Click(object sender, RoutedEventArgs e)
        {
            if (cmbAddStock.SelectedItem is StockAvailableView selectedStock)
            {
                // ТА САМАЯ ПРОВЕРКА: Если менеджер пытается добавить то, что уже закончилось
                if (selectedStock.StockRecord.count <= 0)
                {
                    MessageBox.Show("Этот товар только что закончился на складе! Выберите другой объем памяти или товар.", "Ошибка добавления", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return; // Прерываем выполнение, ничего не добавляем
                }

                // Проверяем, есть ли уже этот товар в заказе
                var existingItem = AppConnect.model1.OrderStock.FirstOrDefault(os => os.idOrder == _currentOrder.id && os.idStock == selectedStock.StockRecord.id);

                if (existingItem != null)
                {
                    existingItem.count++;
                }
                else
                {
                    // Если товара нет, создаем новую запись в чеке
                    var product = AppConnect.model1.Product.FirstOrDefault(p => p.id == selectedStock.StockRecord.idProduct);
                    var newOrderItem = new OrderStock
                    {
                        idOrder = _currentOrder.id,
                        idStock = selectedStock.StockRecord.id,
                        count = 1,
                        price = product.price // Базовая цена
                    };
                    AppConnect.model1.OrderStock.Add(newOrderItem);
                }

                // Вычитаем добавленную единицу со склада
                selectedStock.StockRecord.count--;

                AppConnect.model1.SaveChanges();
                LoadOrderItems();
                LoadAvailableStock(); // Обновляем список, чтобы сразу показать новые остатки
            }
        }


        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentItems.Count == 0)
                {
                    MessageBox.Show("Нельзя сохранить пустой заказ! Добавьте товары из списка ниже или нажмите 'Отмена', чтобы удалить этот заказ.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return; // Останавливаем сохранение
                }

                if (cmbStatus.SelectedItem is StatusOrder selectedStatus)
                {
                    _currentOrder.idStatusOrder = selectedStatus.id;
                }

                foreach (var viewItem in _currentItems)
                {
                    viewItem.OsRecord.price = viewItem.EditPrice;
                }

                AppConnect.model1.SaveChanges();
                MessageBox.Show("Изменения в заказе успешно сохранены!");
                NavigationService.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения: " + ex.Message);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (_currentItems.Count == 0)
            {
                AppConnect.model1.Order.Remove(_currentOrder);
                AppConnect.model1.SaveChanges();
            }
            NavigationService.GoBack();
        }
    }
}
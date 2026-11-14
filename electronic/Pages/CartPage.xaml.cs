using electronic.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace electronic.Pages
{
    public partial class CartPage : Page
    {
        // 1. Вспомогательные классы для красивого вывода в ListView
        public class StockSizeOption
        {
            public int StockId { get; set; }
            public string SizeLabel { get; set; }
            public int MaxCount { get; set; }
        }

        public class CartItemView
        {
            public CartItems CartRecord { get; set; }
            public string ProductName { get; set; }
            public string ImagePath { get; set; }
            public decimal ProductPrice { get; set; }
            public int CurrentCount => CartRecord.count;
            public decimal ItemTotal => ProductPrice * CurrentCount;

            public List<StockSizeOption> AvailableSizes { get; set; }
            public StockSizeOption SelectedSize { get; set; }
            public int StockMaxCount => SelectedSize != null ? SelectedSize.MaxCount : 0;
        }

        public CartPage()
        {
            InitializeComponent();
            LoadCart();
        }

        // 2. Загрузка корзины и размеров со склада
        private void LoadCart()
        {
            if (AppConnect.user == null) return;

            var userCartRecords = AppConnect.model1.CartItems.Where(c => c.idUser == AppConnect.user.id).ToList();
            List<CartItemView> displayList = new List<CartItemView>();
            decimal totalSum = 0;

            foreach (var record in userCartRecords)
            {
                var currentStock = AppConnect.model1.Stock.FirstOrDefault(s => s.id == record.idStock);
                if (currentStock != null)
                {
                    var product = AppConnect.model1.Product.FirstOrDefault(p => p.id == currentStock.idProduct);
                    if (product != null)
                    {
                        // Ищем все варианты этого товара на складе (разные размеры памяти)
                        var allStockForProduct = AppConnect.model1.Stock.Where(s => s.idProduct == product.id && s.count > 0).ToList();
                        List<StockSizeOption> sizes = new List<StockSizeOption>();

                        foreach (var st in allStockForProduct)
                        {
                            string label = "Стандарт";
                            if (st.idSize != null)
                            {
                                var sizeObj = AppConnect.model1.Size.FirstOrDefault(z => z.id == st.idSize);
                                if (sizeObj != null)
                                    label = $"{sizeObj.sizeGb} GB";
                            }
                            sizes.Add(new StockSizeOption { StockId = st.id, SizeLabel = label, MaxCount = st.count });
                        }

                        var viewItem = new CartItemView
                        {
                            CartRecord = record,
                            ProductName = product.name,
                            ImagePath = product.image, // Если используешь конвертер пути, подставь свойство ImagePath
                            ProductPrice = (decimal)product.price,
                            AvailableSizes = sizes
                        };

                        // Выбираем текущий размер
                        viewItem.SelectedSize = sizes.FirstOrDefault(s => s.StockId == record.idStock);

                        displayList.Add(viewItem);
                        totalSum += viewItem.ItemTotal;
                    }
                }
            }

            listCart.ItemsSource = displayList;
            txtTotal.Text = $"{totalSum:N0} ₽";
        }

        // 3. Изменение размера памяти прямо в корзине
        private void cmbSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cmb = sender as ComboBox;
            if (cmb.SelectedItem == null || cmb.Tag == null) return;

            CartItemView viewItem = cmb.Tag as CartItemView;
            StockSizeOption newSize = cmb.SelectedItem as StockSizeOption;

            // Если размер реально изменили
            if (viewItem.CartRecord.idStock != newSize.StockId)
            {
                viewItem.CartRecord.idStock = newSize.StockId;

                // Проверяем, не превышает ли текущее количество товара лимит нового склада
                if (viewItem.CartRecord.count > newSize.MaxCount)
                {
                    viewItem.CartRecord.count = newSize.MaxCount;
                    MessageBox.Show($"Доступно только {newSize.MaxCount} шт. выбранного объема.");
                }

                AppConnect.model1.SaveChanges();
                LoadCart();
            }
        }

        // 4. Кнопки плюс и минус
        private void btnPlus_Click(object sender, RoutedEventArgs e)
        {
            CartItemView viewItem = (sender as Button).DataContext as CartItemView;

            if (viewItem.CurrentCount < viewItem.StockMaxCount)
            {
                viewItem.CartRecord.count++;
                AppConnect.model1.SaveChanges();
                LoadCart();
            }
            else
            {
                MessageBox.Show("Достигнут максимум товара на складе!");
            }
        }

        private void btnMinus_Click(object sender, RoutedEventArgs e)
        {
            CartItemView viewItem = (sender as Button).DataContext as CartItemView;

            if (viewItem.CurrentCount > 1)
            {
                viewItem.CartRecord.count--;
                AppConnect.model1.SaveChanges();
                LoadCart();
            }
            else
            {
                // Если количество становится 0, предлагаем удалить
                if (MessageBox.Show("Удалить товар из корзины?", "Удаление", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    AppConnect.model1.CartItems.Remove(viewItem.CartRecord);
                    AppConnect.model1.SaveChanges();
                    LoadCart();
                }
            }
        }

        // 5. Удаление из корзины
        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            CartItemView viewItem = (sender as Button).DataContext as CartItemView;
            AppConnect.model1.CartItems.Remove(viewItem.CartRecord);
            AppConnect.model1.SaveChanges();
            LoadCart();
        }

        // 6. Оформление заказа (перенос со склада)
        private void btnOrder_Click(object sender, RoutedEventArgs e)
        {
            if (AppConnect.user == null) return;

            var userCartRecords = AppConnect.model1.CartItems.Where(c => c.idUser == AppConnect.user.id).ToList();
            if (userCartRecords.Count == 0)
            {
                MessageBox.Show("Корзина пуста!");
                return;
            }

            try
            {
                Random rnd = new Random();
                int randomOrderNumber = rnd.Next(10000, 99999);

                Order newOrder = new Order
                {
                    idOrder = randomOrderNumber, // Записываем сгенерированный номер
                    date = DateTime.Now,
                    idUser = AppConnect.user.id,
                    idStatusOrder = 4
                };

                AppConnect.model1.Order.Add(newOrder);
                AppConnect.model1.SaveChanges();

                foreach (var cartRecord in userCartRecords)
                {
                    // Создаем запись в заказ
                    var stock = AppConnect.model1.Stock.FirstOrDefault(s => s.id == cartRecord.idStock);
                    var product = AppConnect.model1.Product.FirstOrDefault(p => p.id == stock.idProduct);

                    OrderStock os = new OrderStock
                    {
                        idOrder = newOrder.id,
                        idStock = cartRecord.idStock,
                        count = cartRecord.count,
                        price = product.price
                    };
                    AppConnect.model1.OrderStock.Add(os);

                    // ВЫЧИТАЕМ СО СКЛАДА
                    stock.count -= cartRecord.count;

                    // Удаляем из корзины
                    AppConnect.model1.CartItems.Remove(cartRecord);
                }

                AppConnect.model1.SaveChanges();
                MessageBox.Show("Заказ оформлен!");
                NavigationService.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}